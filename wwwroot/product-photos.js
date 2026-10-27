'use strict';
function productPhotoUrl(product){return product?.imageVersion?'/api/productos/'+product.id+'/imagen?v='+encodeURIComponent(product.imageVersion):'';}
function productPhotoMarkup(product,thumbnail=false){
 const url=productPhotoUrl(product);
 return `<span class="${thumbnail?'product-thumbnail':'product-visual'} ${url?'has-photo':''}">${url?`<img src="${esc(url)}" alt="${esc(product.name)}" loading="lazy" decoding="async">`:''}<span class="photo-fallback" aria-hidden="true">${categoryIcon(product.categoryId)}</span></span>`;
}
async function optimizeProductPhoto(file){
 if(!['image/jpeg','image/png','image/webp'].includes(file.type))throw Error('Elige una imagen JPG, PNG o WebP.');
 if(file.size>10*1024*1024)throw Error('La imagen original no debe superar 10 MB.');
 const source=await new Promise((resolve,reject)=>{const reader=new FileReader();reader.onload=()=>resolve(reader.result);reader.onerror=()=>reject(Error('No se pudo leer la imagen.'));reader.readAsDataURL(file);});
 const image=await new Promise((resolve,reject)=>{const img=new Image();img.onload=()=>resolve(img);img.onerror=()=>reject(Error('No se pudo abrir esa imagen. Elige otra foto.'));img.src=source;});
 if(!image.naturalWidth||!image.naturalHeight||image.naturalWidth*image.naturalHeight>40000000)throw Error('La imagen es demasiado grande. Elige una foto de hasta 40 megapíxeles.');
 let width=Math.min(800,image.naturalWidth),ratio=image.naturalHeight/image.naturalWidth;
 if(width*ratio>800)width=800/ratio;
 const canvas=document.createElement('canvas');const ctx=canvas.getContext('2d');
 if(!ctx)throw Error('Este navegador no permite preparar la foto. Prueba con uno actualizado.');
 for(let attempt=0;attempt<8;attempt++){
  canvas.width=Math.max(1,Math.round(width));canvas.height=Math.max(1,Math.round(width*ratio));ctx.fillStyle='#fff';ctx.fillRect(0,0,canvas.width,canvas.height);ctx.drawImage(image,0,0,canvas.width,canvas.height);
  for(const quality of [.86,.72,.58,.44]){
   const data=canvas.toDataURL('image/jpeg',quality);const encoded=data.split(',')[1];
   if(data.startsWith('data:image/jpeg;')&&encoded.length<=53332)return {base64:encoded,preview:data};
  }
  width*=.75;
 }
 throw Error('No se pudo comprimir la foto. Elige una imagen más sencilla.');
}
function bindProductPhoto(product){
 const file=$('#product-photo'),preview=$('#product-photo-preview'),remove=$('#remove-product-photo'),status=$('#product-photo-status');
 let current=null,pending=Promise.resolve(),failure=null,version=0;
 const original=productPhotoUrl(product);
 const show=src=>{preview.src=src||'';preview.hidden=!src;};show(original);
 file.onchange=()=>{
  const selected=file.files[0],request=++version;current=null;failure=null;
  if(!selected){pending=Promise.resolve();show(remove?.checked?'':original);status.textContent='Sin una foto nueva.';return;}
  if(remove)remove.checked=false;status.textContent='Preparando foto…';
  pending=optimizeProductPhoto(selected).then(result=>{if(request!==version)return;current=result.base64;show(result.preview);status.textContent='Foto lista para guardar.';}).catch(error=>{if(request!==version)return;failure=error;status.textContent=error.message;show(original);});
 };
 if(remove)remove.onchange=()=>{version++;current=null;failure=null;pending=Promise.resolve();file.value='';show(remove.checked?'':original);status.textContent=remove.checked?'La foto se quitará al guardar.':'Se conservará la foto actual.';};
 return async()=>{let task;do{task=pending;await task;}while(task!==pending);if(failure)throw failure;return {imageBase64:current,removeImage:!!remove?.checked};};
}
document.addEventListener('error',event=>{const img=event.target;if(img.tagName==='IMG'&&img.parentElement?.classList.contains('has-photo'))img.parentElement.classList.add('photo-failed');},true);
