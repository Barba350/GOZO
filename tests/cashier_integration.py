"""Pruebas de caja individual, QR, permisos y reportes en PostgreSQL desechable.
Requiere una base VACÍA de CI en GOZO_POSTGRES_CONNECTION. Nunca usa producción.
"""
import concurrent.futures,datetime as dt,http.cookiejar,json,os,pathlib,secrets,socket,subprocess,tempfile,time,urllib.request,urllib.parse,urllib.error,uuid,io,zipfile
from html.parser import HTMLParser
assert os.environ.get('GOZO_DB_PROVIDER')=='PostgreSQL' and os.environ.get('GOZO_ALLOW_TEST_DATABASE')=='true','Configura un PostgreSQL de prueba.'
source=pathlib.Path(__file__).resolve().parents[1]
class Tokens(HTMLParser):
    def __init__(self,html):super().__init__();self.token='';self.feed(html)
    def handle_starttag(self,tag,attrs):
        a=dict(attrs)
        if a.get('name') in ['csrf-token','__RequestVerificationToken']:self.token=a.get('content',a.get('value',''))
class Client:
    def __init__(self):self.browser=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()));self.csrf=''
    def web(self,path,data=None,status=200):
        req=urllib.request.Request(root+path,data=urllib.parse.urlencode(data).encode() if data is not None else None)
        try:r=self.browser.open(req,timeout=30)
        except urllib.error.HTTPError as e:r=e
        html=r.read().decode();assert r.code==status,(path,r.code,html[:300]);return html
    def login(self,name,password):
        token=Tokens(self.web('/Cuenta/Ingresar')).token
        html=self.web('/Cuenta/Ingresar',{'Username':name,'Password':password,'__RequestVerificationToken':token});self.csrf=Tokens(html).token;return html
    def call(self,path,method='GET',data=None,status=200,branch=1,token=None,raw=False):
        headers={'Content-Type':'application/json','X-CSRF-TOKEN':self.csrf if token is None else token,'X-Gozo-Branch':str(branch)}
        req=urllib.request.Request(root+'/api'+path,data=json.dumps(data).encode() if data is not None else None,method=method,headers=headers)
        try:r=self.browser.open(req,timeout=30)
        except urllib.error.HTTPError as e:r=e
        body=r.read();assert r.code==status,(path,status,r.code,body[:600]);return body if raw else json.loads(body) if body else None
with tempfile.TemporaryDirectory(prefix='gozo-cashier-qa-') as temp:
    with socket.socket() as s:s.bind(('127.0.0.1',0));port=s.getsockname()[1]
    root=f'http://127.0.0.1:{port}';pw=secrets.token_urlsafe(24)
    env=dict(os.environ,GOZO_ADMIN_USERNAME='Dueño QA',GOZO_ADMIN_PASSWORD=pw,ASPNETCORE_ENVIRONMENT='Development',DOTNET_ENVIRONMENT='Development',GOZO_DATA_DIR=temp)
    log=open(pathlib.Path(temp)/'server.log','w+')
    p=subprocess.Popen(['dotnet',str(source/'bin/Release/net8.0/GOZO.dll'),'--urls',root],cwd=source,env=env,stdout=log,stderr=subprocess.STDOUT)
    try:
        ready=False
        for _ in range(90):
            if p.poll() is not None:break
            try:urllib.request.urlopen(root+'/salud',timeout=2);ready=True;break
            except OSError:time.sleep(1)
        if not ready:log.seek(0);raise AssertionError('No inició servidor:\n'+log.read())
        owner=Client();owner.login('Dueño QA',pw);assert owner.csrf
        assert owner.call('/productos')==[],'Requiere una base vacía desechable.'
        def cashier(name):
            temporary=secrets.token_urlsafe(24);password=secrets.token_urlsafe(24)
            token=Tokens(owner.web('/Usuarios')).token
            owner.web('/Usuarios/Crear',{'Username':name,'Password':temporary,'Role':'Cajero','__RequestVerificationToken':token})
            c=Client();html=c.login(name,temporary);assert 'CurrentPassword' in html
            c.web('/Cuenta/CambiarClave',{'CurrentPassword':temporary,'NewPassword':password,'ConfirmPassword':password,'__RequestVerificationToken':Tokens(html).token})
            c.login(name,password);return c
        a=cashier('Cajera Ana');b=cashier('Cajero Bruno')
        product=owner.call('/productos','POST',{'name':'Helado QA','price':10,'categoryId':1},201)
        customer=owner.call('/clientes','POST',{'name':'Cliente frecuente','ci':'8765432','contact':'71234567'},201)
        item={'productId':product['id'],'quantity':1}
        def sale(payment='EFECTIVO',coupon=None,quantity=1):return {'customerId':customer['id'],'payment':payment,'items':[{**item,'quantity':quantity}],'requestId':str(uuid.uuid4()),'couponCode':coupon}
        for c in [a,b]:
            for path in ['/reportes','/reportes/opciones','/reportes/pdf','/reportes/excel']:
                c.call(path,status=403)
            html=c.web('/Dashboard');assert 'href="/Reportes"' not in html and 'Mi caja' in html
            c.web('/Reportes',status=403)
            c.call('/operaciones/cupones',status=403)
        a.call('/ventas','POST',sale(),400)
        a.call('/operaciones/caja/abrir','POST',{'openingAmount':100},400,token='')
        a.call('/operaciones/caja/abrir','POST',{'openingAmount':-1},400)
        a.call('/operaciones/caja/abrir','POST',{'openingAmount':100.001},400)
        a.call('/operaciones/caja/abrir','POST',{'openingAmount':100,'denominations':[{'cents':1000,'count':1}]},400)
        ca=a.call('/operaciones/caja/abrir','POST',{'openingAmount':100})
        cb=b.call('/operaciones/caja/abrir','POST',{'openingAmount':50})
        assert ca['id']!=cb['id'] and ca['openedById']!=cb['openedById']
        a.call('/operaciones/caja/abrir','POST',{'openingAmount':0},400,branch=2)
        a.call('/ventas','POST',sale(),400,branch=2)
        b.call('/operaciones/caja/'+str(ca['id']),status=400)
        b.call('/operaciones/caja/'+str(ca['id'])+'/cerrar','POST',{'revision':ca['revision'],'countedCash':100,'countedQr':0,'notes':''},400)
        cash_request=sale(quantity=2);s=a.call('/ventas','POST',cash_request);assert s['total']==2000 and s['seller']=='Cajera Ana' and s['cashSessionId']==ca['id']
        assert a.call('/ventas','POST',cash_request)['id']==s['id']
        a.call('/ventas','POST',sale('QR',quantity=3))
        b.call('/ventas','POST',sale())
        for c,expected_cash,expected_qr in [(a,2000,3000),(b,1000,0)]:
            state=c.call('/operaciones/caja');assert state['cashSales']==expected_cash and state['qrSales']==expected_qr and state['openBoxes']==[]
        dashboard=a.call('/operaciones/dashboard');assert dashboard['products'][0]['quantity']==6 and dashboard['customers'][0]['purchases']==3
        assert 'income' not in dashboard and 'contact' not in json.dumps(dashboard) and 'ci' not in dashboard['customers'][0]
        report=owner.call('/reportes');assert report['income']==6000 and len(report['bySeller'])==2 and report['sales'][0]['branch']=='Sucursal 1'
        seller_id=ca['openedById'];assert owner.call('/reportes?cajero='+str(seller_id))['income']==5000
        assert owner.call('/reportes?sucursal=2')['count']==0
        data=owner.call('/reportes/excel',raw=True)
        with zipfile.ZipFile(io.BytesIO(data)) as z:
            all_xml=''.join(z.read(n).decode() for n in z.namelist() if n.endswith('.xml'));assert 'Cajera Ana' in all_xml and 'Sucursal 1' in all_xml and 'Cajero' in all_xml
        pdf=owner.call('/reportes/pdf',raw=True);assert b'Cajera Ana' in pdf and b'Sucursal 1' in pdf
        change={'revision':s['revision'],'payment':'EFECTIVO','items':[{'productId':product['id'],'quantity':1}],'reason':'Cliente pidió una unidad menos'}
        a.call('/operaciones/ventas/'+str(s['id']),'PUT',change,403)
        edited=owner.call('/operaciones/ventas/'+str(s['id']),'PUT',change);assert edited['total']==1000
        owner.call('/operaciones/ventas/'+str(s['id']),'PUT',change,400)
        assert len(owner.call('/operaciones/ventas/'+str(s['id'])+'/correcciones'))==1
        now=dt.datetime.now(dt.timezone.utc).replace(tzinfo=None)-dt.timedelta(hours=4)
        gift={'code':'REGALO-QA-001','description':'Un helado gratis','type':'PRODUCTO','value':0,'start':(now-dt.timedelta(days=1)).isoformat(),'end':(now+dt.timedelta(days=1)).isoformat(),'customerId':None,'giftProductId':product['id'],'giftQuantity':1}
        owner.call('/operaciones/cupones','POST',gift)
        canje=a.call('/ventas','POST',sale('',gift['code']));assert canje['total']==0 and canje['payment']=='CANJE'
        b.call('/ventas','POST',sale('EFECTIVO',gift['code']),400)
        # Pendiente reserva, cancelar libera, entregar genera una sola venta.
        gift['code']='PEDIDO-QA-001';owner.call('/operaciones/cupones','POST',gift)
        order={'contactName':'QA','contactPhone':'71234567','notes':'Retiro','customerId':customer['id'],'items':[item],'requestId':str(uuid.uuid4()),'couponCode':gift['code']}
        pending=a.call('/pedidos','POST',order);b.call('/ventas','POST',sale('EFECTIVO',gift['code']),400)
        a.call('/pedidos/'+str(pending['id'])+'/cancelar','POST',{})
        canje=b.call('/ventas','POST',sale('',gift['code']));assert canje['total']==0
        order.update(requestId=str(uuid.uuid4()),couponCode=None);pending=a.call('/pedidos','POST',order)
        delivered=a.call('/pedidos/'+str(pending['id'])+'/entregar','POST',{'payment':'QR'})
        assert a.call('/pedidos/'+str(pending['id'])+'/entregar','POST',{'payment':'QR'})['saleId']==delivered['saleId']
        # El cajero declara también el QR. Diferencias no justificadas se rechazan.
        close={'revision':ca['revision'],'countedCash':110,'countedQr':39,'notes':''}
        a.call('/operaciones/caja/'+str(ca['id'])+'/cerrar','POST',close,400)
        close['notes']='QR pendiente de verificar en el banco'
        closed=a.call('/operaciones/caja/'+str(ca['id'])+'/cerrar','POST',close)
        assert closed['expectedAmount']==11000 and closed['qrTotal']==4000 and closed['countedQr']==3900 and closed['qrDifference']==-100 and closed['difference']==0
        a.call('/ventas','POST',sale(),400)
        owner.call('/operaciones/ventas/'+str(s['id']),'PUT',{**change,'revision':edited['revision']},400)
        assert len(a.call('/operaciones/caja')['history'])==1 and len(b.call('/operaciones/caja')['history'])==0
        a.call('/operaciones/caja/'+str(cb['id']),status=400)
        # Nuevo turno en otra sucursal y atribución real de sus ventas.
        ca2=a.call('/operaciones/caja/abrir','POST',{'openingAmount':0},branch=2)
        s2=a.call('/ventas','POST',sale(),branch=2);assert s2['branchId']==2 and s2['cashSessionId']==ca2['id']
        assert owner.call('/reportes?sucursal=2')['income']==1000
        # Cerrar mientras otra petición cobra conserva una conciliación consistente.
        a.call('/operaciones/caja/'+str(ca2['id'])+'/cerrar','POST',{'revision':ca2['revision'],'countedCash':10,'countedQr':0,'notes':''},branch=2)
        assert a.call('/operaciones/dashboard',branch=2)['products'][0]['quantity']==1
        print('PASS: cajas individuales, aislamiento por usuario, cierre efectivo/QR, reportes solo dueño, atribución/filtros/exportación, dashboard sin ingresos, cupones de producto, pedidos y correcciones auditadas.')
    except Exception:
        log.flush();log.seek(0);print(log.read()[-16000:]);raise
    finally:
        p.terminate()
        try:p.wait(timeout=15)
        except subprocess.TimeoutExpired:p.kill()
        log.close()
