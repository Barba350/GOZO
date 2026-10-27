// Prueba del comportamiento del ticket en un DOM simulado. Node.js, sin paquetes.
const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const nodes=new Map(),handlers={};let lastRequest;
const node=s=>{if(!nodes.has(s))nodes.set(s,{content:'qa-csrf',innerHTML:'',value:'',textContent:'',disabled:false,hidden:false,dataset:{},style:{},classList:{toggle(){},add(){},remove(){}},setAttribute(){},addEventListener(){},insertAdjacentHTML(position,html){this.innerHTML+=html;}});return nodes.get(s);};
const context=vm.createContext({console,Intl,Date,URLSearchParams,setTimeout,crypto:require('node:crypto').webcrypto,confirm:()=>true,location:{pathname:'/Ventas'},history:{pushState(){}},window:{addEventListener(){}},document:{body:{dataset:{page:'venta'}},querySelector:node,querySelectorAll:()=>[],addEventListener:(name,handler)=>handlers[name]=handler},fetch:async(url,options)=>{lastRequest=options;return {ok:true,status:200,json:async()=>({subtotal:1000,discount:0,total:1000,lines:[{productId:1,promotion:''}]})};}});
vm.runInContext(fs.readFileSync(require('node:path').join(__dirname,'../wwwroot/product-photos.js'),'utf8'),context);
let source=fs.readFileSync(require('node:path').join(__dirname,'../wwwroot/app.js'),'utf8');
source=source.slice(0,source.indexOf("$('#today').textContent="));vm.runInContext(source,context);
const run=s=>vm.runInContext(s,context);
const click=dataset=>handlers.click({target:{closest:()=>({dataset,matches:()=>false,disabled:false})}});
(async()=>{
 run("products=[{id:1,name:'Helado',price:1000,categoryId:1}];categories=[{id:1,name:'Helados'}];customers=[{id:1,name:'Ana',ci:'1234567',contact:'70000000'},{id:2,name:'Ana',ci:'7654321',contact:'70000000'}];salePage();");
 assert.match(node('#content').innerHTML,/>Ticket</);assert.match(node('#content').innerHTML,/Vaciar ticket/);assert.match(node('#totals').innerHTML,/Bs 0.00/);assert.equal(node('#checkout').disabled,true);
 await click({add:'1'});assert.equal(run('cart.length'),1);assert.equal(node('#checkout').disabled,true,'No debe cobrar sin pago');
 await click({payment:'QR'});assert.equal(run('payment'),'QR');assert.equal(node('#checkout').disabled,false);
 node('#sale-client-search').value='7654321';run('renderClientResults()');assert.match(node('#client-results').innerHTML,/7654321/);assert.doesNotMatch(node('#client-results').innerHTML,/1234567/);
 await click({selectClient:'2'});assert.equal(run('customerId'),'2');assert.match(node('#selected-client').innerHTML,/7654321/);
 await click({remove:'1'});assert.equal(run('cart.length'),0);assert.match(node('#totals').innerHTML,/Bs 0.00/);assert.equal(node('#checkout').disabled,true);
 await click({add:'1'});await click({action:'clear-ticket'});assert.equal(run('cart.length'),0);assert.equal(run('customerId'),'');assert.equal(run('payment'),'');assert.equal(node('#checkout').disabled,true);
 await run("api('/productos/1','DELETE')");assert.equal(lastRequest.headers['X-CSRF-TOKEN'],'qa-csrf','DELETE sin cuerpo debe incluir CSRF');
 // Seleccionar todos, desmarcar y estado parcial de clientes de una promoción.
 const master=node('#promotion-all-customers'),clientChecks=[{checked:false},{checked:false}];
 context.document.querySelectorAll=selector=>selector==='#editor input[name="customers"]'?clientChecks:[];
 run('bindPromotionCustomerSelection()');
 master.checked=true;master.onchange();assert.ok(clientChecks.every(c=>c.checked));
 clientChecks[0].checked=false;clientChecks[0].onchange();assert.equal(master.checked,false);assert.equal(master.indeterminate,true);
 master.checked=false;master.onchange();assert.ok(clientChecks.every(c=>!c.checked));assert.equal(master.indeterminate,false);
 clientChecks.forEach(c=>{c.checked=true;c.onchange();});assert.equal(master.checked,true);assert.equal(master.indeterminate,false);
 assert.match(run("productPhotoMarkup({id:7,name:'Foto real',categoryId:1,imageVersion:'abc'})"),/\/api\/productos\/7\/imagen\?v=abc/);
 assert.doesNotMatch(run("productPhotoMarkup({id:7,name:'Sin foto',categoryId:1})"),/<img/);
 await assert.rejects(()=>run("optimizeProductPhoto({type:'image/svg+xml',size:10})"),/JPG, PNG o WebP/);
 await assert.rejects(()=>run("optimizeProductPhoto({type:'image/jpeg',size:11000000})"),/10 MB/);
 console.log('OK: ticket vacío, total cero, carrito, pago obligatorio, búsqueda CI, selección de homónimos y vaciar ticket.');
})().catch(error=>{console.error(error);process.exitCode=1;});
