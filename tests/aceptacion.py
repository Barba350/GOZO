"""Pruebas de integración sobre una COPIA temporal. Requiere SDK .NET 8 y Python 3.
Ejecutar desde GOZO: python tests/aceptacion.py
Nunca utiliza ni modifica App_Data/gozo.db del proyecto original.
"""
import datetime as dt
import io
import os
import http.cookiejar
import secrets
from html.parser import HTMLParser
import urllib.parse
import json
import pathlib
import shutil
import socket
import subprocess
import tempfile
import time
import urllib.error
import urllib.request
import uuid
import zipfile

if not shutil.which('dotnet'):
    raise SystemExit('Falta el SDK .NET 8. Instálalo antes de ejecutar las pruebas.')
source = pathlib.Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory(prefix='gozo-qa-') as folder:
    project = pathlib.Path(folder)/'GOZO'
    shutil.copytree(source, project, ignore=shutil.ignore_patterns('bin','obj','App_Data','.vs','__pycache__'))
    with socket.socket() as sock:
        sock.bind(('127.0.0.1',0)); port=sock.getsockname()[1]
    base=f'http://127.0.0.1:{port}/api'
    log=open(pathlib.Path(folder)/'servidor.log','w+')
    owner_password=secrets.token_urlsafe(24)
    test_env=dict(os.environ,ASPNETCORE_ENVIRONMENT='Development',DOTNET_ENVIRONMENT='Development',GOZO_ADMIN_USERNAME='Luis Heredia',GOZO_ADMIN_PASSWORD=owner_password,GOZO_DATA_DIR=str(project/'App_Data'))
    process=subprocess.Popen(['dotnet','run','--project',str(project/'GOZO.csproj'),'--no-launch-profile','--','--urls',f'http://127.0.0.1:{port}'],cwd=project,stdout=log,stderr=subprocess.STDOUT,env=test_env)
    root=base.removesuffix('/api')
    owner=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    owner_csrf=''
    class Tokens(HTMLParser):
        def __init__(self,html):
            super().__init__();self.token='';self.feed(html)
        def handle_starttag(self,tag,attributes):
            a=dict(attributes)
            if tag=='input' and a.get('name')=='__RequestVerificationToken':self.token=a.get('value','')
            if tag=='meta' and a.get('name')=='csrf-token':self.token=a.get('content','')
    def web(opener,path,data=None,status=200):
        req=urllib.request.Request(root+path,data=urllib.parse.urlencode(data).encode() if data is not None else None)
        try:r=opener.open(req,timeout=30)
        except urllib.error.HTTPError as error:r=error
        html=r.read().decode();assert r.code==status,(path,status,r.code,html[:200]);return html
    def login(opener,username,password):
        token=Tokens(web(opener,'/Cuenta/Ingresar')).token
        return web(opener,'/Cuenta/Ingresar',{'Username':username,'Password':password,'__RequestVerificationToken':token})
    def call(path,method='GET',data=None,status=200,raw=False,opener=None,csrf=None):
        body=json.dumps(data).encode() if data is not None else None
        req=urllib.request.Request(base+path,data=body,method=method,headers={'X-CSRF-TOKEN':owner_csrf if csrf is None else csrf,**({'Content-Type':'application/json'} if body else {})})
        try:
            response=(opener or owner).open(req,timeout=30)
        except urllib.error.HTTPError as error:
            response=error
        result=response.read()
        assert response.code==status,(path,status,response.code,result.decode(errors='replace'))
        return result if raw or not result else json.loads(result)
    try:
        ready=False
        for _ in range(240):
            if process.poll() is not None:break
            try:urllib.request.urlopen(root+'/salud',timeout=2).read();ready=True;break
            except (OSError,AssertionError):time.sleep(1)
        if not ready:
            log.seek(0);raise AssertionError('No se pudo iniciar ASP.NET:\n'+log.read())
        call('/categorias',status=401)
        html=login(owner,' Luis   Heredia ',owner_password);owner_csrf=Tokens(html).token;assert owner_csrf
        assert 'Dueno' in html
        call('/productos','POST',{'name':'No CSRF','price':10,'categoryId':1},400,csrf='')
        for route,key in [('Ventas','venta'),('Productos','productos'),('Clientes','clientes'),('Promociones','promociones'),('Reportes','reportes'),('Historial','historial')]:
            html=owner.open(root+'/'+route).read().decode()
            assert f'data-page="{key}"' in html and 'gozo-logo.png' in html,(route,'Vista Razor incompleta')
        assert urllib.request.urlopen(base.removesuffix('/api')+'/images/gozo-logo.png').read().startswith(b'\x89PNG\r\n\x1a\n')
        cats=call('/categorias');assert [c['name'] for c in cats]==['Helados', 'Hamburguesas', 'Bebidas', 'Sándwiches y croissants', 'Salchipapas', 'Platos de pollo', 'Empanadas', 'Cafetería', 'Jugos naturales', 'Postres']
        p=call('/productos','POST',{'name':'Helado QA','price':10,'categoryId':cats[0]['id']},201)
        p2=call('/productos','POST',{'name':'Bebida QA','price':5,'categoryId':cats[2]['id']},201)
        call('/productos','POST',{'name':'  HELADO   QA ','price':10,'categoryId':cats[0]['id']},400)
        call('/productos','POST',{'name':'Sin precio','price':0,'categoryId':cats[0]['id']},400)
        call('/productos','POST',{'name':'Sin categoría','price':5,'categoryId':999},400)
        for invalid_phone in ['ana@example.com','abcdefgh','7123456','712345678','7123 456']:
            call('/clientes','POST',{'name':'Dato inválido','ci':'9000001','contact':invalid_phone},400)
        c=call('/clientes','POST',{'name':'Ana Perez','ci':'1234567','contact':'70000001'},201)
        c2=call('/clientes','POST',{'name':'Ana Perez','ci':'7654321','contact':'70000001'},201)
        call('/clientes','POST',{'name':'Otra persona','ci':'1.234.567','contact':'70000002'},400)
        assert call('/clientes?buscar=1234567')[0]['id']==c['id']
        assert len(call('/clientes?buscar=Ana'))==2
        assert len(call('/categorias'))==10
        def sale(items,customer=None,payment='EFECTIVO'):
            return {'customerId':customer,'payment':payment,'items':items,'requestId':str(uuid.uuid4())}
        items=[{'productId':p['id'],'quantity':2},{'productId':p2['id'],'quantity':1}]
        request=sale(items)
        request['payment']='';call('/ventas','POST',request,400)
        request['payment']='EFECTIVO';first=call('/ventas','POST',request);assert first['total']==2500 and first['customerId'] is None
        assert call('/ventas','POST',request)['id']==first['id'] # Reintento no duplica.
        call('/productos/'+str(p['id']),'PUT',{'name':'Helado QA nuevo','price':12,'categoryId':cats[0]['id']})
        call('/productos/'+str(p2['id']),'PUT',{'name':'Helado QA nuevo','price':5,'categoryId':cats[0]['id']},400)
        second=call('/ventas','POST',sale([{'productId':p['id'],'quantity':1}],c['id'],'QR'));assert second['total']==1200
        now=(dt.datetime.now(dt.timezone.utc)-dt.timedelta(hours=4)).replace(tzinfo=None)
        promo={'name':'Promo QA','description':'Descuento de prueba','type':'PORCENTAJE','value':10,'start':(now-dt.timedelta(days=1)).isoformat(),'end':(now+dt.timedelta(days=1)).isoformat(),'active':True,'productIds':[p['id']],'customerIds':[c['id']]}
        pr=call('/promociones','POST',promo)
        assert any(x['id']==pr['id'] for x in call('/promociones?activas=true'))
        third=call('/ventas','POST',sale([{'productId':p['id'],'quantity':2}],c['id']));assert third['total']==2160 and third['discount']==240
        other=call('/ventas/cotizar','POST',sale([{'productId':p['id'],'quantity':1}],c2['id']));assert other['discount']==0
        # Editar relaciones conservadas y cambiar porcentaje a monto por unidad.
        promo['type']='MONTO';promo['value']=3
        call('/promociones/'+str(pr['id']),'PUT',promo)
        assert call('/ventas/cotizar','POST',sale([{'productId':p['id'],'quantity':2}],c['id']))['total']==1800
        promo['value']=999
        call('/promociones/'+str(pr['id']),'PUT',promo)
        assert call('/ventas/cotizar','POST',sale([{'productId':p['id'],'quantity':1}],c['id']))['total']==0
        call('/promociones/'+str(pr['id']),'DELETE',status=204)
        assert call('/ventas/cotizar','POST',sale([{'productId':p['id'],'quantity':1}],c['id']))['discount']==0
        promo['value']=10;promo['end']=(now-dt.timedelta(hours=1)).isoformat()
        call('/promociones','POST',promo)
        assert call('/ventas/cotizar','POST',sale([{'productId':p['id'],'quantity':1}],c['id']))['discount']==0
        report=call('/reportes?desde=2000-01-01&hasta=2100-01-01')
        assert report['count']==3 and report['income']==5860
        assert sum(x['total'] for x in report['payments'])==5860
        assert report['ranking'][0]['quantity']==5
        historical=next(x for x in report['sales'] if x['id']==first['id'])
        historical_line=next(x for x in historical['lines'] if x['productId']==p['id'])
        assert historical_line['unitPrice']==1000
        assert historical_line['name']=='Helado QA'
        history=call(f'/clientes/{c["id"]}/historial?desde=2000-01-01&hasta=2100-01-01')['report'];assert history['income']==3360 and history['count']==2
        assert call(f'/clientes/{c2["id"]}/historial')['report']['count']==0
        call('/reportes?desde=2100-01-01&hasta=2000-01-01',status=400)
        assert call('/reportes?desde=2000-01-01&hasta=2000-01-01')['count']==0
        pdf=call('/reportes/pdf?desde=2000-01-01&hasta=2100-01-01',raw=True);assert pdf.startswith(b'%PDF-1.4') and b'58.60' in pdf
        excel=call('/reportes/excel?desde=2000-01-01&hasta=2100-01-01',raw=True)
        with zipfile.ZipFile(io.BytesIO(excel)) as z:assert 'xl/workbook.xml' in z.namelist()
        call('/productos/'+str(p['id']),'DELETE',status=204)
        assert all(x['id']!=p['id'] for x in call('/productos'))
        assert call('/reportes?desde=2000-01-01&hasta=2100-01-01')['income']==5860
        call('/ventas','POST',sale([{'productId':p['id'],'quantity':1}]),400)
        # Crear cajero desde la vista, exigir cambio de clave y comprobar permisos.
        temp_password=secrets.token_urlsafe(24);cashier_password=secrets.token_urlsafe(24)
        users_html=web(owner,'/Usuarios');token=Tokens(users_html).token
        users_html=web(owner,'/Usuarios/Crear',{'Username':'qacashier','Password':temp_password,'Role':'Cajero','__RequestVerificationToken':token})
        assert 'qacashier' in users_html
        import sqlite3
        def account_db():
            if os.environ.get('GOZO_DB_PROVIDER') == 'PostgreSQL':
                import psycopg
                class Adapter:
                    def __init__(self): self.connection=psycopg.connect(os.environ['GOZO_TEST_PG_DSN'])
                    def execute(self,query,args=()):
                        # Solo las consultas estáticas de comprobación de esta prueba.
                        import re
                        query=re.sub(r'\b(Accounts|Events|Id|Username|PasswordHash|LockedUntil|Action)\b',r'"\1"',query)
                        query=query.replace('"Accounts"','gozo_accounts."Accounts"').replace('"Events"','gozo_accounts."Events"')
                        return self.connection.execute(query.replace('?','%s'),args)
                    def close(self): self.connection.close()
                return Adapter()
            return sqlite3.connect(project/'App_Data/accounts.db')
        db=account_db()
        cashier_id=db.execute('select Id from Accounts where Username=?',('qacashier',)).fetchone()[0]
        password_hash=db.execute('select PasswordHash from Accounts where Id=?',(cashier_id,)).fetchone()[0]
        assert password_hash!=temp_password and len(password_hash)>60
        db.close()
        cashier=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
        html=login(cashier,'qacashier',temp_password);assert 'CurrentPassword' in html
        call('/categorias',status=403,opener=cashier)
        token=Tokens(html).token
        web(cashier,'/Cuenta/CambiarClave',{'CurrentPassword':temp_password,'NewPassword':cashier_password,'ConfirmPassword':cashier_password,'__RequestVerificationToken':token})
        html=login(cashier,'qacashier',cashier_password);cashier_csrf=Tokens(html).token;assert cashier_csrf
        call('/categorias',opener=cashier)
        assert call('/reportes?desde=2000-01-01&hasta=2100-01-01',opener=cashier)['income']==5860
        assert call('/reportes/pdf?desde=2000-01-01&hasta=2100-01-01',opener=cashier,raw=True).startswith(b'%PDF-1.4')
        with zipfile.ZipFile(io.BytesIO(call('/reportes/excel?desde=2000-01-01&hasta=2100-01-01',opener=cashier,raw=True))) as z:assert 'xl/workbook.xml' in z.namelist()
        report_html=web(cashier,'/Reportes')
        assert 'data-page="reportes"' in report_html and 'href="/Reportes"' in report_html
        call(f'/clientes/{c["id"]}/historial',status=403,opener=cashier)
        web(cashier,'/Historial',status=403)
        web(cashier,'/Productos',status=403)
        web(cashier,'/Promociones',status=403)
        call('/productos','POST',{'name':'No permitido','price':10,'categoryId':1},403,opener=cashier,csrf=cashier_csrf)
        web(cashier,'/Usuarios',status=403)
        # CSRF en logout: no se acepta cerrar por POST sin token.
        web(cashier,'/Cuenta/Salir',{},status=400)
        # Bloqueo persistente de un usuario activo después de cinco contraseñas erróneas.
        for _ in range(5):login(owner,'qacashier','incorrect-password')
        db=account_db()
        assert db.execute('select LockedUntil from Accounts where Id=?',(cashier_id,)).fetchone()[0] is not None
        assert db.execute('select count(*) from Events where Action=?',('Acceso rechazado',)).fetchone()[0]>=5
        db.close()
        # Revocación efectiva de la cookie al desactivar al cajero.
        token=Tokens(web(owner,'/Usuarios')).token
        web(owner,'/Usuarios/Estado',{'id':cashier_id,'__RequestVerificationToken':token})
        call('/categorias',status=401,opener=cashier)
        # Cabeceras y ausencia de caché en vistas privadas.
        response=owner.open(root+'/Ventas');response.read()
        assert response.headers.get('X-Frame-Options')=='DENY'
        assert "script-src 'self'" in response.headers.get('Content-Security-Policy','')
        assert 'no-store' in response.headers.get('Cache-Control','')
        # Reiniciar el proceso conserva datos y permite reutilizar la misma cookie.
        process.terminate();process.wait(timeout=15)
        process=subprocess.Popen(['dotnet','run','--project',str(project/'GOZO.csproj'),'--no-launch-profile','--','--urls',f'http://127.0.0.1:{port}'],cwd=project,stdout=log,stderr=subprocess.STDOUT,env=test_env)
        restarted=False
        for _ in range(120):
            if process.poll() is not None:break
            try:urllib.request.urlopen(root+'/salud',timeout=2).read();restarted=True;break
            except OSError:time.sleep(1)
        if not restarted:
            log.seek(0);raise AssertionError('No reinició: '+log.read())
        assert call('/reportes?desde=2000-01-01&hasta=2100-01-01')['income']==5860
        assert len(call('/clientes?buscar=Ana'))==2
        assert len(call('/categorias'))==10
        if os.environ.get('GOZO_DB_PROVIDER', 'SQLite') == 'SQLite':
            # Regresión: formulario CSRF con cookies Secure detrás de terminación TLS.
            process.terminate();process.wait(timeout=15)
            edge_env=dict(test_env,ASPNETCORE_ENVIRONMENT='Production',DOTNET_ENVIRONMENT='Production',GOZO_PERSISTENT_SQLITE='true',GOZO_PUBLIC_ORIGIN=f'https://127.0.0.1:{port}',GOZO_TLS_TERMINATION='Edge')
            process=subprocess.Popen(['dotnet','run','--project',str(project/'GOZO.csproj'),'--no-launch-profile','--','--urls',f'http://127.0.0.1:{port}'],cwd=project,stdout=log,stderr=subprocess.STDOUT,env=edge_env)
            edge_ready=False
            for _ in range(120):
                if process.poll() is not None:break
                try:urllib.request.urlopen(root+'/salud',timeout=2).read();edge_ready=True;break
                except OSError:time.sleep(1)
            if not edge_ready:
                log.seek(0);raise AssertionError('No inició Edge: '+log.read())
            response=urllib.request.urlopen(root+'/Cuenta/Ingresar',timeout=30)
            assert response.code==200 and Tokens(response.read().decode()).token
            cookie=response.headers.get('Set-Cookie','').lower()
            assert '__host-gozo.csrf=' in cookie and 'secure' in cookie and 'samesite=strict' in cookie
        print('OK: formulario HTTPS Edge, reinicio conserva datos y sesión; autenticación, CSRF, roles, cambio de clave, revocación, cabeceras; catálogo, CI único y homónimos, venta, pagos, históricos, promociones, reportes, PDF, XLSX y baja lógica.')
    finally:
        process.terminate()
        try:process.wait(timeout=15)
        except subprocess.TimeoutExpired:process.kill();process.wait()
        log.close()
