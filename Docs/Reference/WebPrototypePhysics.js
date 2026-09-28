export const SIZE = 520;
export const CENTER = 260;
export const BOUNDARY = 237;
export const SPLIT_SPEED = 330;
export const STAGES = [
  { title: '돌아가는 길', description: '돌을 돌아 초록 원 안으로. <br>하나로 합친 뒤 손을 놓아주세요.', drops: [[150,155,25],[335,145,27],[142,310,23],[356,325,25]], obstacles: [{x:260,y:250,r:46}], goal:{x:260,y:382,r:67}, splitLimit:2, splitSpeed:330, current:0, gate:null },
  { title: '합치기 전에', description: '커진 방울은 문을 지날 수 없어요. <br>먼저 옮길지, 먼저 합칠지 생각해요.', drops: [[145,130,24],[260,105,23],[375,135,24],[211,182,23],[315,184,24]], obstacles: [{x:70,y:260,r:43},{x:155,y:260,r:60},{x:365,y:260,r:60},{x:450,y:260,r:43}], goal:{x:260,y:382,r:66}, splitLimit:2, splitSpeed:310, current:0, gate:{x:260,y:260,width:90} },
  { title: '작은 것부터', description: '오른쪽 좁은 문을 찾아보세요. <br>작을 때 건너고, 건넌 뒤 모아요.', drops: [[145,130,21],[235,95,22],[330,111,21],[390,181,20],[230,173,21],[110,186,20]], obstacles: [{x:70,y:260,r:48},{x:166,y:260,r:58},{x:226,y:260,r:49},{x:395,y:260,r:50},{x:450,y:260,r:44}], goal:{x:270,y:384,r:64}, splitLimit:1, splitSpeed:290, current:0, gate:{x:310,y:260,width:70} },
  { title: '흐름을 거슬러', description: '물살이 방울을 옆으로 밀어요. <br>왼쪽 문을 지나 안전하게 모으세요.', drops: [[150,118,21],[243,95,23],[335,118,21],[388,169,22],[285,178,21],[205,170,22]], obstacles: [{x:90,y:260,r:76},{x:295,y:260,r:61},{x:400,y:260,r:62},{x:460,y:260,r:35}], goal:{x:285,y:385,r:66}, splitLimit:1, splitSpeed:270, current:12, gate:{x:200,y:260,width:68} },
  { title: '흔들림 없이', description: '가장 좁은 문, 더 강해진 물살. <br>단 한 번의 갈라짐도 없이 도전해요.', drops: [[150,115,19],[238,89,20],[327,109,19],[391,164,18],[310,172,19],[213,158,20],[110,166,19]], obstacles: [{x:65,y:260,r:48},{x:156,y:260,r:76},{x:364,y:260,r:76},{x:455,y:260,r:48}], goal:{x:260,y:391,r:63}, splitLimit:0, splitSpeed:250, current:18, gate:{x:260,y:260,width:56} }
];

export class Simulation {
  constructor(stage = 0) { this.reset(stage); }
  reset(stage) {
    this.stage = stage; this.nextId = 0; this.time = 0; this.splits = 0; this.merges = 0;
    this.pointer = null; this.dragId = null; this.speed = 0; this.stress = 0; this.settle = 0; this.won = false; this.failed = false; this.events = []; this.blocked = false;
    this.config = STAGES[stage]; this.goal = this.config.goal;
    this.obstacles = this.config.obstacles;
    this.drops = STAGES[stage].drops.map(([x,y,r]) => this.makeDrop(x,y,r*r));
    this.initialMass = this.drops.reduce((sum,d) => sum+d.mass,0);
  }
  makeDrop(x,y,mass) { return {id:this.nextId++,x,y,mass,r:Math.sqrt(mass),vx:0,vy:0,cooldown:0,stretch:0,angle:0,phase:this.nextId*1.91}; }
  grab(x,y) {
    if(this.won || this.failed) return false;
    const candidates = this.drops.filter(d => Math.hypot(d.x-x,d.y-y)<d.r+17).sort((a,b) => Math.hypot(a.x-x,a.y-y)-Math.hypot(b.x-x,b.y-y));
    if(!candidates.length) return false;
    const d = candidates[0]; this.dragId=d.id; this.pointer={x,y,offsetX:d.x-x,offsetY:d.y-y,lastX:x,lastY:y}; this.stress=0;
    return true;
  }
  move(x,y) { if(this.pointer) { this.pointer.x=x; this.pointer.y=y; } }
  release() { this.pointer=null; this.dragId=null; this.stress=0; }
  split(d) {
    const angle=Math.atan2(d.vy,d.vx), ux=Math.cos(angle),uy=Math.sin(angle);
    const childMass=d.mass*.35; d.mass-=childMass; d.r=Math.sqrt(d.mass);
    const childR=Math.sqrt(childMass), separation=d.r+childR+7;
    const child=this.makeDrop(d.x-ux*separation,d.y-uy*separation,childMass);
    child.vx=-ux*24;child.vy=-uy*24;child.cooldown=1.1;d.cooldown=1.1;
    this.drops.push(child);this.constrain(child);this.splits++;this.stress=0;
    this.events.push({type:'split',x:d.x,y:d.y});
    if(this.splits>this.config.splitLimit) {this.failed=true;this.release();this.events.push({type:'fail'});}
  }
  get splitThreshold() {
    const d=this.drops.find(d=>d.id===this.dragId);
    return this.config.splitSpeed * (d?Math.max(.67,1-(d.r-20)*.009):1);
  }
  get inGoal() {
    const d=this.drops[0];return this.drops.length===1 && Math.hypot(d.x-this.goal.x,d.y-this.goal.y)+d.r<=this.goal.r;
  }
  validPosition(d) {
    return Math.hypot(d.x-CENTER,d.y-CENTER)<=BOUNDARY-d.r+.01 && this.obstacles.every(o=>Math.hypot(d.x-o.x,d.y-o.y)>=o.r+d.r*.89-.01);
  }
  constrain(d) {
    for(let pass=0;pass<4;pass++) {
      for(const o of this.obstacles) {
        const dx=d.x-o.x,dy=d.y-o.y,dist=Math.hypot(dx,dy),min=o.r+d.r*.89;
        if(dist<min) { const nx=dist?dx/dist:1,ny=dist?dy/dist:0;d.x=o.x+nx*min;d.y=o.y+ny*min;const vn=d.vx*nx+d.vy*ny;if(vn<0){d.vx-=vn*nx;d.vy-=vn*ny;} }
      }
      const dx=d.x-CENTER,dy=d.y-CENTER,dist=Math.hypot(dx,dy),max=BOUNDARY-d.r;
      if(dist>max) { d.x=CENTER+dx/dist*max;d.y=CENTER+dy/dist*max;const vn=(d.vx*dx+d.vy*dy)/dist;if(vn>0){d.vx-=vn*dx/dist;d.vy-=vn*dy/dist;} }
    }
  }
  advance(d,dt) {
    // Short collision steps prevent a large drop being pushed through a narrow gate.
    const steps=Math.max(1,Math.ceil(Math.hypot(d.vx,d.vy)*dt/3));
    for(let i=0;i<steps;i++) {
      const x=d.x,y=d.y;
      d.x+=d.vx*dt/steps;d.y+=d.vy*dt/steps;this.constrain(d);
      if(!this.validPosition(d) || Math.hypot(d.x-x,d.y-y)>6) {d.x=x;d.y=y;d.vx=0;d.vy=0;if(d.id===this.dragId)this.blocked=true;break;}
    }
  }
  clearPath(a,b) {
    return this.obstacles.every(o=>{
      const dx=b.x-a.x,dy=b.y-a.y,t=Math.max(0,Math.min(1,((o.x-a.x)*dx+(o.y-a.y)*dy)/(dx*dx+dy*dy||1)));
      return Math.hypot(o.x-a.x-t*dx,o.y-a.y-t*dy)>o.r+3;
    });
  }
  step(dt) {
    if(this.won || this.failed || dt<=0) return;
    dt=Math.min(dt,1/30);this.time+=dt;this.blocked=false;
    let inputSpeed=0;
    if(this.pointer) { const p=this.pointer;inputSpeed=Math.hypot(p.x-p.lastX,p.y-p.lastY)/dt;p.lastX=p.x;p.lastY=p.y; }
    this.speed+=(Math.min(inputSpeed,1400)-this.speed)*(1-Math.exp(-dt*12));
    for(const d of this.drops) {
      d.cooldown=Math.max(0,d.cooldown-dt);
      if(d.id===this.dragId && this.pointer) {
        const dx=this.pointer.x+this.pointer.offsetX-d.x,dy=this.pointer.y+this.pointer.offsetY-d.y;
        d.vx+=dx*74*dt;d.vy+=dy*74*dt;
        const speed=Math.hypot(d.vx,d.vy),limit=570;if(speed>limit){d.vx*=limit/speed;d.vy*=limit/speed;}
        if(this.speed>this.splitThreshold && speed>115) this.stress+=dt;else this.stress=Math.max(0,this.stress-dt*.6);
        if(this.stress>.085 && d.cooldown===0 && d.mass>320 && this.drops.length<28) this.split(d);
      } else { d.vx+=Math.sin(this.time*.5+d.phase)*1.8*dt;d.vy+=Math.cos(this.time*.6+d.phase)*1.8*dt; }
      if(this.config.current) {
        const sheltered=Math.hypot(d.x-this.goal.x,d.y-this.goal.y)<this.goal.r ? .02 : (d.id===this.dragId?1:.25);
        d.vx+=Math.sin(this.time*.65+d.y*.009)*this.config.current*4*sheltered*dt;
        d.vy+=Math.cos(this.time*.45+d.x*.009)*this.config.current*1.5*sheltered*dt;
      }
      const damping=Math.exp(-dt*(d.id===this.dragId?10:4));d.vx*=damping;d.vy*=damping;
      this.advance(d,dt);
      const speed=Math.hypot(d.vx,d.vy);d.stretch+=(Math.min(.34,speed/1200)-d.stretch)*(1-Math.exp(-dt*9));if(speed>3)d.angle=Math.atan2(d.vy,d.vx);
    }
    if(this.failed)return;
    for(let i=0;i<this.drops.length;i++)for(let j=i+1;j<this.drops.length;j++) {
      const a=this.drops[i],b=this.drops[j],dx=b.x-a.x,dy=b.y-a.y,dist=Math.hypot(dx,dy),sum=a.r+b.r;
      if(a.cooldown>0||b.cooldown>0||!this.clearPath(a,b))continue;
      if(dist<sum*.88) {
        const mass=a.mass+b.mass,dragged=a.id===this.dragId?a:b.id===this.dragId?b:null;
        const merged=this.makeDrop((a.x*a.mass+b.x*b.mass)/mass,(a.y*a.mass+b.y*b.mass)/mass,mass);
        // Oil cannot grow inside stone or jump across a gate when it merges.
        if(!this.validPosition(merged))continue;
        merged.vx=(a.vx*a.mass+b.vx*b.mass)/mass;merged.vy=(a.vy*a.mass+b.vy*b.mass)/mass;merged.stretch=.16;merged.angle=Math.atan2(dy,dx);
        if(dragged){this.dragId=merged.id;this.pointer.offsetX=merged.x-this.pointer.x;this.pointer.offsetY=merged.y-this.pointer.y;}
        this.drops.splice(j,1);this.drops.splice(i,1,merged);this.constrain(merged);this.merges++;this.events.push({type:'merge',x:merged.x,y:merged.y});i--;break;
      } else if(dist<sum+15 && dist>0) {
        const f=(1-(dist-sum)/15)*22*dt;a.vx+=dx/dist*f;a.vy+=dy/dist*f;b.vx-=dx/dist*f;b.vy-=dy/dist*f;
      }
    }
    if(this.inGoal && !this.pointer) {this.settle+=dt;if(this.settle>1.2){this.won=true;this.events.push({type:'win'});}}
    else this.settle=0;
  }
}
