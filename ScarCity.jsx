import { useState, useEffect, useRef, useCallback } from "react";

const TOTAL_TIME = 180;
const MAX_HP = 5;
const MAX_SCORE = 10000;
const CRISIS_TYPES = [
  { id: "earthquake", name: "Earthquake!", color: "#ff6b35", icon: "⚠️", timeLimit: 30 },
  { id: "forestfire", name: "Forest Fire!", color: "#ff3333", icon: "🔥", timeLimit: 35 },
  { id: "treeplanting", name: "Reforest Zone", color: "#44cc44", icon: "🌱", timeLimit: 40 },
];
const SPAWN_POINTS = [
  {x:22,y:18},{x:55,y:12},{x:78,y:25},{x:15,y:55},
  {x:42,y:68},{x:70,y:60},{x:30,y:82},{x:60,y:80},{x:85,y:45},
];

function liveScore(hp, timeLeft) {
  if (hp <= 0) return 0;
  return Math.round(MAX_SCORE * (hp / MAX_HP) * (0.5 + 0.5 * (timeLeft / TOTAL_TIME)));
}

// ── Crisis Marker ──────────────────────────────────────────────────────────
function CrisisMarker({ crisis, onClick }) {
  const [blink, setBlink] = useState(false);
  const urgent = crisis.timeLeft < 10;
  useEffect(() => {
    const iv = setInterval(() => setBlink(b => !b), urgent ? 280 : 600);
    return () => clearInterval(iv);
  }, [urgent]);
  const pct = crisis.timeLeft / crisis.crisisType.timeLimit;
  const color = urgent ? "#ff2222" : crisis.crisisType.color;
  const dash = 2 * Math.PI * 22;
  return (
    <div onClick={onClick} style={{
      position:"absolute", left:`${crisis.x}%`, top:`${crisis.y}%`,
      transform:"translate(-50%,-50%)", cursor:"pointer", zIndex:10,
      transition:"transform 0.1s",
    }}
      onMouseEnter={e=>e.currentTarget.style.transform="translate(-50%,-50%) scale(1.18)"}
      onMouseLeave={e=>e.currentTarget.style.transform="translate(-50%,-50%) scale(1)"}
    >
      <div style={{position:"absolute",inset:-8,borderRadius:"50%",border:`2px solid ${color}`,opacity:blink?0.8:0.15,transition:"opacity 0.25s"}}/>
      <div style={{width:44,height:44,borderRadius:"50%",background:`radial-gradient(circle at 35% 35%,${color}bb,${color}33)`,border:`2px solid ${color}`,display:"flex",alignItems:"center",justifyContent:"center",fontSize:19,boxShadow:`0 0 18px ${color}99`,position:"relative"}}>
        {crisis.crisisType.icon}
        <svg style={{position:"absolute",inset:-4,width:52,height:52}}>
          <circle cx="26" cy="26" r="22" fill="none" stroke={color} strokeWidth="2.5"
            strokeDasharray={`${dash*pct} ${dash}`} strokeLinecap="round"
            style={{transform:"rotate(-90deg)",transformOrigin:"center"}}/>
        </svg>
      </div>
      <div style={{position:"absolute",top:"100%",left:"50%",transform:"translateX(-50%)",marginTop:4,whiteSpace:"nowrap",background:"rgba(0,0,0,0.88)",border:`1px solid ${color}55`,borderRadius:3,padding:"2px 7px",color,fontSize:10,fontWeight:"bold",letterSpacing:1,fontFamily:"'Courier New',monospace"}}>
        {crisis.crisisType.name} {Math.ceil(crisis.timeLeft)}s
      </div>
    </div>
  );
}

// ── City Map ───────────────────────────────────────────────────────────────
function CityMap({ crises, onCrisisClick, hp, timeLeft, score }) {
  const pct = timeLeft / TOTAL_TIME;
  const tc = pct > 0.5 ? "#44ff88" : pct > 0.25 ? "#ffcc00" : "#ff4444";
  const blocks = [
    {x:8,y:5,w:18,h:12},{x:30,y:5,w:22,h:14},{x:57,y:5,w:15,h:10},{x:75,y:8,w:16,h:16},
    {x:5,y:22,w:12,h:20},{x:20,y:25,w:20,h:18},{x:45,y:22,w:18,h:15},{x:68,y:28,w:22,h:20},
    {x:5,y:50,w:25,h:22},{x:35,y:48,w:16,h:20},{x:55,y:52,w:20,h:18},{x:80,y:52,w:15,h:18},
    {x:8,y:76,w:18,h:16},{x:32,y:74,w:24,h:18},{x:60,y:75,w:18,h:16},{x:82,y:74,w:14,h:16},
  ];
  const roads = [
    {x:0,y:19,w:100,h:3},{x:0,y:45,w:100,h:3},{x:0,y:71,w:100,h:3},
    {x:18,y:0,w:3,h:100},{x:43,y:0,w:3,h:100},{x:67,y:0,w:3,h:100},{x:80,y:0,w:3,h:100},
  ];
  return (
    <div style={{position:"relative",width:"100%",height:"100%",background:"#1a2e1a",overflow:"hidden",fontFamily:"'Courier New',monospace"}}>
      <svg style={{position:"absolute",inset:0,width:"100%",height:"100%",opacity:0.12}}>
        {Array.from({length:11}).map((_,i)=>(
          <g key={i}>
            <line x1={`${(i+1)*8.5}%`} y1="0" x2={`${(i+1)*8.5}%`} y2="100%" stroke="#44ff88" strokeWidth="0.5"/>
            <line x1="0" y1={`${(i+1)*8.5}%`} x2="100%" y2={`${(i+1)*8.5}%`} stroke="#44ff88" strokeWidth="0.5"/>
          </g>
        ))}
      </svg>
      {blocks.map((b,i)=>(
        <div key={i} style={{position:"absolute",left:`${b.x}%`,top:`${b.y}%`,width:`${b.w}%`,height:`${b.h}%`,background:i%3===0?"#1e3a1e":i%3===1?"#1a351a":"#162c16",border:"1px solid #2a4a2a",borderRadius:2}}/>
      ))}
      {roads.map((r,i)=>(
        <div key={i} style={{position:"absolute",left:`${r.x}%`,top:`${r.y}%`,width:`${r.w}%`,height:`${r.h}%`,background:"#0d1a0d",borderTop:"1px solid #1a2a1a",borderBottom:"1px solid #1a2a1a"}}/>
      ))}
      {crises.map(c=><CrisisMarker key={c.id} crisis={c} onClick={()=>onCrisisClick(c)}/>)}
      {/* HUD */}
      <div style={{position:"absolute",top:12,left:12,right:12,display:"flex",justifyContent:"space-between",alignItems:"flex-start",pointerEvents:"none"}}>
        <div style={{background:"rgba(0,0,0,0.88)",border:`1.5px solid ${tc}`,borderRadius:4,padding:"6px 16px",color:tc,fontSize:22,fontWeight:"bold",letterSpacing:2,boxShadow:`0 0 14px ${tc}44`}}>
          {Math.floor(timeLeft/60)}:{String(Math.floor(timeLeft%60)).padStart(2,"0")}
        </div>
        <div style={{background:"rgba(0,0,0,0.88)",border:"1.5px solid #44aaff",borderRadius:4,padding:"5px 14px",color:"#44aaff",fontSize:17,fontWeight:"bold",textAlign:"center",boxShadow:"0 0 14px #44aaff44"}}>
          <div style={{fontSize:9,color:"#4488aa",letterSpacing:2,marginBottom:1}}>SCORE</div>
          {score.toLocaleString()}
        </div>
        <div style={{background:"rgba(0,0,0,0.88)",border:"1.5px solid #ff4444",borderRadius:4,padding:"6px 12px",boxShadow:"0 0 14px #ff444444"}}>
          <div style={{fontSize:9,color:"#aa4444",letterSpacing:2,marginBottom:3,textAlign:"center"}}>HP</div>
          <div style={{display:"flex",gap:4}}>
            {Array.from({length:MAX_HP}).map((_,i)=>(
              <div key={i} style={{width:13,height:13,borderRadius:"50%",background:i<hp?"#ff4444":"#331111",boxShadow:i<hp?"0 0 6px #ff4444":"none",transition:"all 0.3s"}}/>
            ))}
          </div>
        </div>
      </div>
      <div style={{position:"absolute",bottom:12,left:"50%",transform:"translateX(-50%)",background:"rgba(0,0,0,0.7)",border:"1px solid #2a4a2a",borderRadius:4,padding:"5px 16px",color:"#44ff8866",fontSize:10,letterSpacing:1,whiteSpace:"nowrap"}}>
        CLICK CRISIS MARKERS TO RESPOND
      </div>
    </div>
  );
}

// ── Earthquake Minigame ────────────────────────────────────────────────────
const EQ_STATIONS = [
  {name:"STN-A",x:20,y:33,color:"#22ddff"},
  {name:"STN-B",x:67,y:25,color:"#ff9922"},
  {name:"STN-C",x:42,y:75,color:"#88ff33"},
];
const ACTUAL_EPI = {x:44,y:44};
const ACC_ERR = 8;

function circleInts(x1,y1,r1,x2,y2,r2){
  const d=Math.hypot(x2-x1,y2-y1);
  if(d>r1+r2||d<Math.abs(r1-r2)||d===0)return[];
  const a=(r1*r1-r2*r2+d*d)/(2*d);
  const h=Math.sqrt(Math.max(0,r1*r1-a*a));
  const mx=x1+a*(x2-x1)/d, my=y1+a*(y2-y1)/d;
  const px=-(y2-y1)/d, py=(x2-x1)/d;
  const pts=[{x:mx+h*px,y:my+h*py}];
  if(h>0.001)pts.push({x:mx-h*px,y:my-h*py});
  return pts;
}
function circumctr(a,b,c){
  const D=2*(a.x*(b.y-c.y)+b.x*(c.y-a.y)+c.x*(a.y-b.y));
  if(Math.abs(D)<0.0001)return{x:(a.x+b.x+c.x)/3,y:(a.y+b.y+c.y)/3};
  const ux=((a.x*a.x+a.y*a.y)*(b.y-c.y)+(b.x*b.x+b.y*b.y)*(c.y-a.y)+(c.x*c.x+c.y*c.y)*(a.y-b.y))/D;
  const uy=((a.x*a.x+a.y*a.y)*(c.x-b.x)+(b.x*b.x+b.y*b.y)*(a.x-c.x)+(c.x*c.x+c.y*c.y)*(b.x-a.x))/D;
  return{x:ux,y:uy};
}

function EarthquakeMinigame({onComplete}){
  const [radii,setRadii]=useState([0,0,0]);
  const [locked,setLocked]=useState([false,false,false]);
  const [activeIdx,setActiveIdx]=useState(0);
  const [dragging,setDragging]=useState(false);
  const [result,setResult]=useState(null);
  const [guess,setGuess]=useState(null);
  const svgRef=useRef(null);

  const svgPos=useCallback((e)=>{
    const r=svgRef.current.getBoundingClientRect();
    return{x:((e.clientX-r.left)/r.width)*100,y:((e.clientY-r.top)/r.height)*100};
  },[]);

  const onMove=useCallback((e)=>{
    if(!dragging||locked[activeIdx])return;
    const p=svgPos(e);
    const st=EQ_STATIONS[activeIdx];
    const d=Math.hypot(p.x-st.x,p.y-st.y);
    setRadii(r=>{const n=[...r];n[activeIdx]=Math.max(2,Math.min(45,d));return n;});
  },[dragging,activeIdx,locked,svgPos]);

  const onUp=useCallback(()=>{
    if(!dragging)return;
    setDragging(false);
    if(radii[activeIdx]>2){
      setLocked(l=>{const n=[...l];n[activeIdx]=true;return n;});
      if(activeIdx<2)setActiveIdx(activeIdx+1);
    }
  },[dragging,activeIdx,radii]);

  const submit=()=>{
    const pts=[[0,1],[1,2],[0,2]].map(([i,j])=>{
      const ints=circleInts(EQ_STATIONS[i].x,EQ_STATIONS[i].y,radii[i],EQ_STATIONS[j].x,EQ_STATIONS[j].y,radii[j]);
      if(!ints.length)return null;
      const k=3-i-j;
      let best=ints[0],bd=Infinity;
      ints.forEach(p=>{const d=Math.abs(Math.hypot(p.x-EQ_STATIONS[k].x,p.y-EQ_STATIONS[k].y)-radii[k]);if(d<bd){bd=d;best=p;}});
      return best;
    });
    if(pts.some(p=>!p)){setResult("miss");setTimeout(()=>onComplete(false),2000);return;}
    const ctr=circumctr(pts[0],pts[1],pts[2]);
    const err=Math.hypot(ctr.x-ACTUAL_EPI.x,ctr.y-ACTUAL_EPI.y);
    setGuess(ctr);
    const ok=err<=ACC_ERR;
    setResult(ok?"hit":"miss");
    setTimeout(()=>onComplete(ok),2500);
  };

  const allLocked=locked.every(Boolean);
  const col=result==="hit"?"#44ff88":result==="miss"?"#ff4444":null;

  return(
    <div style={{width:"100%",height:"100%",background:"#0a1a0f",display:"flex",flexDirection:"column",fontFamily:"'Courier New',monospace",userSelect:"none"}}>
      <div style={{padding:"10px 16px",background:"#0d2010",borderBottom:"1px solid #1a4020",display:"flex",justifyContent:"space-between",alignItems:"center"}}>
        <div style={{color:"#ff6b35",fontSize:13,fontWeight:"bold",letterSpacing:2}}>EARTHQUAKE TRIANGULATION</div>
        <div style={{color:"#44ff88aa",fontSize:11}}>
          {allLocked?"HIT SUBMIT WHEN READY":`DRAG FROM ${EQ_STATIONS[activeIdx].name} TO SET P-WAVE RADIUS`}
        </div>
      </div>
      <div style={{flex:1,position:"relative"}}>
        <svg ref={svgRef} style={{width:"100%",height:"100%",cursor:dragging?"crosshair":"default"}}
          onMouseMove={onMove} onMouseUp={onUp} onMouseLeave={onUp}>
          <rect width="100%" height="100%" fill="#0a1a0a"/>
          {Array.from({length:9}).map((_,i)=>(
            <g key={i}>
              <line x1={`${(i+1)*10}%`} y1="0" x2={`${(i+1)*10}%`} y2="100%" stroke="#1a3a1a" strokeWidth="1"/>
              <line x1="0" y1={`${(i+1)*10}%`} x2="100%" y2={`${(i+1)*10}%`} stroke="#1a3a1a" strokeWidth="1"/>
            </g>
          ))}
          <line x1="50%" y1="0" x2="50%" y2="100%" stroke="#2a5a2a" strokeWidth="1.5"/>
          <line x1="0" y1="50%" x2="100%" y2="50%" stroke="#2a5a2a" strokeWidth="1.5"/>
          {EQ_STATIONS.map((st,i)=>(
            <g key={i}>
              {radii[i]>0&&<circle cx={`${st.x}%`} cy={`${st.y}%`} r={`${radii[i]}%`} fill="none" stroke={st.color} strokeWidth={locked[i]?"1.5":"1"} strokeDasharray={locked[i]?"none":"4 2"} opacity={locked[i]?0.7:0.45}/>}
            </g>
          ))}
          {guess&&<circle cx={`${guess.x}%`} cy={`${guess.y}%`} r="1.5%" fill={col} stroke={col} strokeWidth="1" opacity="0.9"/>}
          {result&&(
            <g>
              <circle cx={`${ACTUAL_EPI.x}%`} cy={`${ACTUAL_EPI.y}%`} r="1.8%" fill="none" stroke="#ffff44" strokeWidth="2.5" opacity="0.95"/>
              <line x1={`${ACTUAL_EPI.x-2.5}%`} y1={`${ACTUAL_EPI.y}%`} x2={`${ACTUAL_EPI.x+2.5}%`} y2={`${ACTUAL_EPI.y}%`} stroke="#ffff44" strokeWidth="2"/>
              <line x1={`${ACTUAL_EPI.x}%`} y1={`${ACTUAL_EPI.y-2.5}%`} x2={`${ACTUAL_EPI.x}%`} y2={`${ACTUAL_EPI.y+2.5}%`} stroke="#ffff44" strokeWidth="2"/>
            </g>
          )}
          {EQ_STATIONS.map((st,i)=>(
            <g key={i} onMouseDown={e=>{if(i===activeIdx&&!locked[i]){e.preventDefault();setDragging(true);}}} style={{cursor:i===activeIdx&&!locked[i]?"crosshair":"default"}}>
              <circle cx={`${st.x}%`} cy={`${st.y}%`} r="2.5%" fill={locked[i]?st.color:i===activeIdx?st.color+"66":"#1a2a1a"} stroke={st.color} strokeWidth="2"/>
              <circle cx={`${st.x}%`} cy={`${st.y}%`} r="0.8%" fill={i===activeIdx&&!locked[i]?"#fff":st.color}/>
              <text x={`${st.x+3}%`} y={`${st.y-3}%`} fill={st.color} fontSize="1.8%" fontFamily="Courier New" fontWeight="bold">
                {st.name}{radii[i]>0?` ${radii[i].toFixed(1)}km`:""}
              </text>
            </g>
          ))}
        </svg>
        {result&&(
          <div style={{position:"absolute",inset:0,display:"flex",alignItems:"center",justifyContent:"center",background:"rgba(0,0,0,0.72)",zIndex:20}}>
            <div style={{background:result==="hit"?"#061a06":"#1a0606",border:`2px solid ${col}`,borderRadius:8,padding:"24px 48px",textAlign:"center",color:col,fontFamily:"'Courier New',monospace",boxShadow:`0 0 50px ${col}44`}}>
              <div style={{fontSize:28,fontWeight:"bold",marginBottom:6}}>{result==="hit"?"EPICENTER LOCATED":"TOO FAR OFF"}</div>
              <div style={{fontSize:12,opacity:0.75}}>{result==="hit"?"Triangulation successful! Returning...":"Estimate missed. Taking damage..."}</div>
            </div>
          </div>
        )}
      </div>
      <div style={{padding:"8px 16px",background:"#0d2010",borderTop:"1px solid #1a4020",display:"flex",justifyContent:"space-between",alignItems:"center"}}>
        <div style={{display:"flex",gap:16}}>
          {EQ_STATIONS.map((st,i)=>(
            <div key={i} style={{display:"flex",alignItems:"center",gap:5}}>
              <div style={{width:8,height:8,borderRadius:"50%",background:locked[i]?st.color:"#222",boxShadow:locked[i]?`0 0 6px ${st.color}`:"none"}}/>
              <span style={{color:locked[i]?st.color:"#444",fontSize:11}}>{st.name}</span>
            </div>
          ))}
        </div>
        {allLocked&&!result&&(
          <button onClick={submit} style={{background:"#0a3a1a",border:"1.5px solid #44ff88",color:"#44ff88",padding:"6px 22px",borderRadius:4,fontFamily:"'Courier New',monospace",fontSize:12,fontWeight:"bold",cursor:"pointer",letterSpacing:2}}>
            SUBMIT
          </button>
        )}
      </div>
    </div>
  );
}

// ── Forest Fire Minigame ───────────────────────────────────────────────────
function ForestFireMinigame({onComplete}){
  const initTrees=()=>{
    const ts=[];
    for(let i=0;i<48;i++){
      let x,y,ok,tries=0;
      do{x=5+Math.random()*90;y=10+Math.random()*80;ok=ts.every(t=>Math.hypot(t.x-x,t.y-y)>5.5);tries++;}
      while(!ok&&tries<300);
      ts.push({id:i,x,y,state:"alive",ft:0});
    }
    return ts;
  };
  const [trees,setTrees]=useState(initTrees);
  const [timeLeft,setTimeLeft]=useState(15);
  const [done,setDone]=useState(false);
  const [result,setResult]=useState(null);
  const alive=trees.filter(t=>t.state==="alive").length;
  const hp=alive/48;

  useEffect(()=>{
    if(done)return;
    const iv=setInterval(()=>{
      setTimeLeft(t=>{
        if(t<=1){setDone(true);setResult("ok");setTimeout(()=>onComplete(true),2000);return 0;}
        return t-1;
      });
    },1000);
    return()=>clearInterval(iv);
  },[done]);

  useEffect(()=>{
    if(done)return;
    const iv=setInterval(()=>{
      setTrees(prev=>{
        const alive=prev.filter(t=>t.state==="alive");
        if(!alive.length)return prev;
        const tgt=alive[Math.floor(Math.random()*alive.length)];
        return prev.map(t=>t.id===tgt.id?{...t,state:"fire",ft:Date.now()}:t);
      });
    },2000);
    return()=>clearInterval(iv);
  },[done]);

  useEffect(()=>{
    if(done)return;
    const iv=setInterval(()=>{
      setTrees(prev=>{
        let changed=false;
        const next=prev.map(t=>{
          if(t.state==="fire"&&t.ft&&Date.now()-t.ft>1400){changed=true;return{...t,state:"burned",ft:0};}
          return t;
        });
        return changed?next:prev;
      });
    },150);
    return()=>clearInterval(iv);
  },[done]);

  const click=id=>{
    if(done)return;
    setTrees(prev=>prev.map(t=>t.id===id&&t.state==="fire"?{...t,state:"alive",ft:0}:t));
  };

  const treeC=s=>s==="alive"?"#22aa44":s==="fire"?"#ff6600":"#443311";

  return(
    <div style={{width:"100%",height:"100%",background:"#0a1a0a",display:"flex",flexDirection:"column",fontFamily:"'Courier New',monospace"}}>
      <div style={{padding:"10px 16px",background:"#0d1a0d",borderBottom:"1px solid #1a4020",display:"flex",justifyContent:"space-between",alignItems:"center"}}>
        <div style={{color:"#ff4422",fontSize:13,fontWeight:"bold",letterSpacing:2}}>FOREST FIRE RESPONSE</div>
        <div style={{color:timeLeft<=5?"#ff2222":"#ff9922",fontSize:14,fontWeight:"bold"}}>⏱ {timeLeft}s</div>
      </div>
      <div style={{padding:"7px 16px",background:"#0a180a",display:"flex",alignItems:"center",gap:10}}>
        <span style={{color:"#44aa44",fontSize:11}}>FOREST HP</span>
        <div style={{flex:1,height:7,background:"#111",borderRadius:4,overflow:"hidden"}}>
          <div style={{width:`${hp*100}%`,height:"100%",background:hp>0.6?"#44ff44":hp>0.3?"#ffaa00":"#ff4444",transition:"width 0.4s",borderRadius:4}}/>
        </div>
        <span style={{color:"#44aa44",fontSize:11,minWidth:32}}>{Math.round(hp*100)}%</span>
      </div>
      <div style={{flex:1,position:"relative"}}>
        <svg style={{width:"100%",height:"100%"}}>
          <rect width="100%" height="100%" fill="#0d1a0d"/>
          {trees.map(t=>(
            <g key={t.id} onClick={()=>click(t.id)} style={{cursor:t.state==="fire"?"pointer":"default"}}>
              <circle cx={`${t.x}%`} cy={`${t.y}%`} r={t.state==="fire"?"2.2%":"1.5%"} fill={treeC(t.state)} opacity={t.state==="burned"?0.45:1}/>
              {t.state==="fire"&&(
                <>
                  <circle cx={`${t.x}%`} cy={`${t.y}%`} r="3.5%" fill="none" stroke="#ff6600" strokeWidth="1.5" opacity="0.4"/>
                  <text x={`${t.x}%`} y={`${t.y+0.7}%`} textAnchor="middle" fontSize="2.8%" style={{userSelect:"none"}}>🔥</text>
                </>
              )}
            </g>
          ))}
        </svg>
        {result&&(
          <div style={{position:"absolute",inset:0,display:"flex",alignItems:"center",justifyContent:"center",background:"rgba(0,0,0,0.72)",zIndex:20}}>
            <div style={{background:"#061a06",border:"2px solid #44ff88",borderRadius:8,padding:"24px 48px",textAlign:"center",color:"#44ff88",fontFamily:"'Courier New',monospace"}}>
              <div style={{fontSize:26,fontWeight:"bold"}}>FIRES CONTAINED</div>
              <div style={{fontSize:13,opacity:0.75,marginTop:6}}>Forest HP: {Math.round(hp*100)}% — Returning...</div>
            </div>
          </div>
        )}
      </div>
      <div style={{padding:"7px 16px",background:"#0d1a0d",borderTop:"1px solid #1a3020",color:"#44aa4466",fontSize:10,letterSpacing:1,textAlign:"center"}}>
        CLICK BURNING TREES BEFORE THEY SPREAD
      </div>
    </div>
  );
}

// ── Tree Replanting Memory ─────────────────────────────────────────────────
function TreeReplantingMinigame({onComplete}){
  const TARGET_ROUNDS=5;
  const [round,setRound]=useState(1);
  const [phase,setPhase]=useState("memorize");
  const [targets,setTargets]=useState(()=>Array.from({length:25},(_,i)=>i).sort(()=>Math.random()-0.5).slice(0,1));
  const [selected,setSelected]=useState([]);
  const [revealed,setRevealed]=useState([]);
  const [msg,setMsg]=useState("Memorize the highlighted tile!");
  const tmr=useRef(null);

  useEffect(()=>{
    tmr.current=setTimeout(()=>{setPhase("recall");setMsg(`Click the ${targets.length} correct tile${targets.length>1?"s":""}!`);},2500);
    return()=>clearTimeout(tmr.current);
  },[targets]);

  const nextRound=(r)=>{
    setSelected([]);setRevealed([]);
    const t=Array.from({length:25},(_,i)=>i).sort(()=>Math.random()-0.5).slice(0,r);
    setTargets(t);setPhase("memorize");setMsg(`Memorize ${r} tile${r>1?"s":""}!`);
    tmr.current=setTimeout(()=>{setPhase("recall");setMsg(`Click the ${r} correct tile${r>1?"s":""}!`);},2500);
  };

  const click=(idx)=>{
    if(phase!=="recall")return;
    if(selected.includes(idx))return;
    const ns=[...selected,idx];
    setSelected(ns);
    if(!targets.includes(idx)){
      setPhase("result");setRevealed(ns);setMsg("WRONG TILE — Mission failed!");
      setTimeout(()=>onComplete(false),2000);return;
    }
    if(ns.filter(s=>targets.includes(s)).length===targets.length){
      setPhase("result");setRevealed(ns);
      if(round>=TARGET_ROUNDS){setMsg("ALL ROUNDS COMPLETE!");setTimeout(()=>onComplete(true),2000);}
      else{setMsg("CORRECT! Next round...");setTimeout(()=>{const nr=round+1;setRound(nr);nextRound(nr);},1500);}
    }else{
      setMsg(`${targets.length-ns.filter(s=>targets.includes(s)).length} more to go...`);
    }
  };

  const tileC=(i)=>{
    if(phase==="memorize"&&targets.includes(i))return"#22cc44";
    if(revealed.includes(i)){
      if(targets.includes(i)&&selected.includes(i))return"#22cc44";
      if(!targets.includes(i)&&selected.includes(i))return"#cc2222";
      if(targets.includes(i))return"#226622";
    }
    if(selected.includes(i))return"#bbcc22";
    return"#0d200d";
  };

  return(
    <div style={{width:"100%",height:"100%",background:"#0a1a0a",display:"flex",flexDirection:"column",fontFamily:"'Courier New',monospace"}}>
      <div style={{padding:"10px 16px",background:"#0d1a0d",borderBottom:"1px solid #1a4020",display:"flex",justifyContent:"space-between",alignItems:"center"}}>
        <div style={{color:"#44ff88",fontSize:13,fontWeight:"bold",letterSpacing:2}}>REFORESTATION MEMORY</div>
        <div style={{color:"#44aa44",fontSize:11}}>ROUND {round} / {TARGET_ROUNDS}</div>
      </div>
      <div style={{flex:1,display:"flex",flexDirection:"column",alignItems:"center",justifyContent:"center",gap:16,padding:20}}>
        <div style={{color:"#44cc88",fontSize:13,letterSpacing:1,textAlign:"center",minHeight:20}}>{msg}</div>
        <div style={{display:"grid",gridTemplateColumns:"repeat(5,1fr)",gap:6,width:"min(260px,75vw)"}}>
          {Array.from({length:25}).map((_,i)=>(
            <div key={i} onClick={()=>click(i)} style={{
              aspectRatio:"1",background:tileC(i),
              border:`1.5px solid ${phase==="memorize"&&targets.includes(i)?"#44ff88":"#1a3a1a"}`,
              borderRadius:5,cursor:phase==="recall"?"pointer":"default",
              display:"flex",alignItems:"center",justifyContent:"center",fontSize:17,
              transition:"all 0.15s",boxShadow:phase==="memorize"&&targets.includes(i)?"0 0 12px #44ff8855":"none",
            }}>
              {phase==="memorize"&&targets.includes(i)?"🌱":
               revealed.includes(i)&&targets.includes(i)?"🌳":
               revealed.includes(i)&&!targets.includes(i)&&selected.includes(i)?"✗":""}
            </div>
          ))}
        </div>
        <div style={{display:"flex",gap:6,marginTop:4}}>
          {Array.from({length:TARGET_ROUNDS}).map((_,i)=>(
            <div key={i} style={{width:9,height:9,borderRadius:"50%",background:i<round-1?"#44ff88":i===round-1?"#44aa44":"#1a3a1a",boxShadow:i<round?"0 0 5px #44ff8877":"none",transition:"all 0.3s"}}/>
          ))}
        </div>
      </div>
    </div>
  );
}

// ── Main ───────────────────────────────────────────────────────────────────
export default function ScarCity(){
  const [screen,setScreen]=useState("title");
  const [hp,setHp]=useState(MAX_HP);
  const [timeLeft,setTimeLeft]=useState(TOTAL_TIME);
  const [crises,setCrises]=useState([]);
  const [active,setActive]=useState(null);
  const [gameOn,setGameOn]=useState(false);
  const cidRef=useRef(0);
  const occRef=useRef(new Set());

  const score=liveScore(hp,timeLeft);

  useEffect(()=>{
    if(!gameOn)return;
    const iv=setInterval(()=>{
      setTimeLeft(t=>{
        if(t<=1){setGameOn(false);setScreen("gameover");return 0;}
        return t-0.1;
      });
    },100);
    return()=>clearInterval(iv);
  },[gameOn]);

  useEffect(()=>{
    if(!gameOn)return;
    let to;
    const spawn=()=>{
      const free=SPAWN_POINTS.map((_,i)=>i).filter(i=>!occRef.current.has(i));
      if(free.length){
        const pi=free[Math.floor(Math.random()*free.length)];
        const pt=SPAWN_POINTS[pi];
        const type=CRISIS_TYPES[Math.floor(Math.random()*CRISIS_TYPES.length)];
        const id=cidRef.current++;
        occRef.current.add(pi);
        setCrises(c=>[...c,{id,x:pt.x,y:pt.y,crisisType:type,timeLeft:type.timeLimit,spawnIdx:pi}]);
      }
      to=setTimeout(spawn,8000+Math.random()*8000);
    };
    to=setTimeout(spawn,4000);
    return()=>clearTimeout(to);
  },[gameOn]);

  useEffect(()=>{
    if(!gameOn)return;
    const iv=setInterval(()=>{
      setCrises(prev=>{
        const next=[];
        for(const c of prev){
          if(c.timeLeft<=0){
            occRef.current.delete(c.spawnIdx);
            setHp(h=>{
              const nh=Math.max(0,h-1);
              if(nh<=0){setGameOn(false);setScreen("gameover");}
              return nh;
            });
          }else next.push({...c,timeLeft:c.timeLeft-0.1});
        }
        return next;
      });
    },100);
    return()=>clearInterval(iv);
  },[gameOn]);

  const start=()=>{
    setHp(MAX_HP);setTimeLeft(TOTAL_TIME);setCrises([]);setActive(null);
    occRef.current=new Set();cidRef.current=0;
    setGameOn(true);setScreen("city");
  };

  const clickCrisis=(c)=>{
    setActive(c);setGameOn(false);
    setCrises(cs=>cs.filter(x=>x.id!==c.id));
    occRef.current.delete(c.spawnIdx);
    setScreen("minigame");
  };

  const miniDone=(ok)=>{
    if(!ok)setHp(h=>{
      const nh=Math.max(0,h-1);
      if(nh<=0){setActive(null);setScreen("gameover");return nh;}
      return nh;
    });
    setActive(null);setGameOn(true);setScreen("city");
  };

  const final=liveScore(hp,Math.floor(timeLeft));

  const BtnStyle=(col)=>({
    background:"transparent",border:`2px solid ${col}`,color:col,
    padding:"13px 48px",borderRadius:4,fontFamily:"'Courier New',monospace",
    fontSize:15,fontWeight:"bold",letterSpacing:4,cursor:"pointer",
    boxShadow:`0 0 22px ${col}44`,transition:"all 0.2s",
  });

  return(
    <div style={{width:"100%",height:"100vh",background:"#050d05",fontFamily:"'Courier New',monospace",overflow:"hidden"}}>
      {screen==="title"&&(
        <div style={{width:"100%",height:"100%",display:"flex",flexDirection:"column",alignItems:"center",justifyContent:"center",background:"radial-gradient(ellipse at center,#0a2a0a 0%,#050d05 100%)",position:"relative"}}>
          <div style={{position:"absolute",inset:0,backgroundImage:"repeating-linear-gradient(0deg,transparent,transparent 2px,rgba(0,0,0,0.12) 2px,rgba(0,0,0,0.12) 4px)",pointerEvents:"none"}}/>
          <div style={{fontSize:11,letterSpacing:8,color:"#44ff8855",marginBottom:14}}>EMERGENCY RESPONSE SIMULATOR</div>
          <div style={{fontWeight:900,color:"#44ff88",letterSpacing:-2,textShadow:"0 0 40px #44ff8877,0 0 80px #44ff8822",lineHeight:1,fontSize:"clamp(40px,8vw,72px)"}}>
            SCAR<span style={{color:"#ff6b35"}}>CITY</span>
          </div>
          <div style={{fontSize:11,letterSpacing:6,color:"#44ff8844",marginBottom:44,marginTop:8}}>STEM CRISIS RESPONSE</div>
          <div style={{display:"flex",gap:18,marginBottom:48,flexWrap:"wrap",justifyContent:"center"}}>
            {CRISIS_TYPES.map(t=>(
              <div key={t.id} style={{background:"#0a1a0a",border:`1px solid ${t.color}33`,borderRadius:6,padding:"10px 18px",textAlign:"center",minWidth:90}}>
                <div style={{fontSize:24}}>{t.icon}</div>
                <div style={{fontSize:10,color:t.color,letterSpacing:1,marginTop:4}}>{t.name}</div>
              </div>
            ))}
          </div>
          <button style={BtnStyle("#44ff88")} onClick={start}
            onMouseEnter={e=>{e.target.style.background="#44ff8822";e.target.style.boxShadow="0 0 40px #44ff8877";}}
            onMouseLeave={e=>{e.target.style.background="transparent";e.target.style.boxShadow="0 0 22px #44ff8844";}}>
            DEPLOY
          </button>
          <div style={{position:"absolute",bottom:18,color:"#44ff8822",fontSize:10,letterSpacing:2}}>MADE BY GRADE 8 STUDENTS · SCARCITY</div>
        </div>
      )}

      {screen==="city"&&<CityMap crises={crises} onCrisisClick={clickCrisis} hp={hp} timeLeft={timeLeft} score={score}/>}

      {screen==="minigame"&&active&&(
        <div style={{width:"100%",height:"100%",position:"relative"}}>
          {active.crisisType.id==="earthquake"&&<EarthquakeMinigame onComplete={miniDone}/>}
          {active.crisisType.id==="forestfire"&&<ForestFireMinigame onComplete={miniDone}/>}
          {active.crisisType.id==="treeplanting"&&<TreeReplantingMinigame onComplete={miniDone}/>}
        </div>
      )}

      {screen==="gameover"&&(
        <div style={{width:"100%",height:"100%",display:"flex",flexDirection:"column",alignItems:"center",justifyContent:"center",background:"radial-gradient(ellipse at center,#1a0505 0%,#050505 100%)",gap:14,position:"relative"}}>
          <div style={{position:"absolute",inset:0,backgroundImage:"repeating-linear-gradient(0deg,transparent,transparent 2px,rgba(0,0,0,0.12) 2px,rgba(0,0,0,0.12) 4px)",pointerEvents:"none"}}/>
          <div style={{fontSize:10,letterSpacing:8,color:"#ff444455"}}>MISSION COMPLETE</div>
          <div style={{fontSize:"clamp(36px,7vw,60px)",fontWeight:900,color:"#ff4444",textShadow:"0 0 40px #ff444466",letterSpacing:-1}}>GAME OVER</div>
          <div style={{background:"#0d0505",border:"1px solid #ff444433",borderRadius:8,padding:"22px 40px",textAlign:"center",marginTop:10}}>
            <div style={{fontSize:10,color:"#ff444466",letterSpacing:4,marginBottom:6}}>FINAL SCORE</div>
            <div style={{fontSize:"clamp(36px,7vw,52px)",fontWeight:"bold",color:"#ffcc44",textShadow:"0 0 20px #ffcc4433"}}>{final.toLocaleString()}</div>
            <div style={{display:"flex",gap:28,marginTop:14,justifyContent:"center"}}>
              <div style={{textAlign:"center"}}><div style={{fontSize:9,color:"#ff444466",letterSpacing:2}}>HP LEFT</div><div style={{fontSize:22,color:"#ff4444"}}>{hp}/{MAX_HP}</div></div>
              <div style={{textAlign:"center"}}><div style={{fontSize:9,color:"#ff444466",letterSpacing:2}}>TIME LEFT</div><div style={{fontSize:22,color:"#ff4444"}}>{Math.floor(timeLeft)}s</div></div>
            </div>
          </div>
          <button style={{...BtnStyle("#ff4444"),marginTop:12}} onClick={start}
            onMouseEnter={e=>{e.target.style.background="#ff444422";}}
            onMouseLeave={e=>{e.target.style.background="transparent";}}>
            RETRY
          </button>
        </div>
      )}
    </div>
  );
}
