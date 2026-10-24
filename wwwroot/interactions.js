'use strict';
// Efectos progresivos: se conserva el cursor nativo y se respeta movimiento reducido.
const motion = window.matchMedia('(prefers-reduced-motion: reduce)');
const finePointer = window.matchMedia('(hover: hover) and (pointer: fine)');
const halo = document.querySelector('.cursor-halo');
let pointerX=0,pointerY=0,frame=0;
document.addEventListener('pointermove',event=>{
 if(motion.matches||!finePointer.matches)return;
 pointerX=event.clientX;pointerY=event.clientY;
 const card=event.target.closest('.product');
 if(card){const rect=card.getBoundingClientRect();card.style.setProperty('--px',(event.clientX-rect.left)+'px');card.style.setProperty('--py',(event.clientY-rect.top)+'px');}
 if(!frame)frame=requestAnimationFrame(()=>{halo.style.transform=`translate(${pointerX-215}px,${pointerY-215}px)`;frame=0;});
},{passive:true});
document.addEventListener('pointerdown',event=>{
 if(motion.matches)return;
 const target=event.target.closest('.product');if(!target)return;
 const rect=target.getBoundingClientRect(),ripple=document.createElement('span');
 ripple.className='tap-ripple';ripple.setAttribute('aria-hidden','true');ripple.style.left=(event.clientX-rect.left-8)+'px';ripple.style.top=(event.clientY-rect.top-8)+'px';target.append(ripple);ripple.addEventListener('animationend',()=>ripple.remove(),{once:true});
});
// Solo anima cambios de pantalla completos; no reinicia al editar cantidades.
const content=document.querySelector('#content');
const observer=new MutationObserver(records=>{if(motion.matches||!records.some(r=>r.target===content))return;content.classList.remove('view-enter');requestAnimationFrame(()=>content.classList.add('view-enter'));});
observer.observe(content,{childList:true});

const menuToggle=document.querySelector('.menu-toggle');
menuToggle?.addEventListener('click',()=>{const open=document.body.classList.toggle('nav-open');menuToggle.setAttribute('aria-expanded',String(open));});

// Volver con Atrás debe revalidar la sesión en el servidor.
window.addEventListener('pageshow',event=>{if(event.persisted)location.reload();});
