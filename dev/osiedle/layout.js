// Independent level 2. Coordinates in metres, north = negative Z.
export const LEVEL = {
 id: 'osiedle', version: 'stage-2-architecture', spawn: [0, 1.7, 52],
 bounds: {minX:-114,maxX:114,minZ:-102,maxZ:99},
 villa: {x:0,z:-38,width:24,depth:16},
 houses: [
  {id:'dom-01',model:'dom_suburban',x:-28,z:27,width:17,rotation:Math.PI/2},
  {id:'dom-02',model:'dom_nowoczesny',x:27,z:28,width:14,rotation:-Math.PI/2},
  {id:'dom-03',model:'willa_nowoczesna',x:-29,z:0,width:17,rotation:Math.PI/2},
  {id:'dom-04',model:'dom_z_wnetrzem',x:28,z:0,width:14,rotation:-Math.PI/2},
  {id:'dom-07',model:'dom_wnetrze_zewn',x:-28,z:59,width:20,rotation:0},
  {id:'dom-08',model:'dom_nowoczesny',x:28,z:59,width:15,rotation:-Math.PI/2},
 ],
 vegetation:[
  ...[[10,-73,6.1],[24,-74,6.6],[40,-73,5.8],[49,-90,6.4],[34,-94,6.8],[15,-94,5.6],[-48,-95,5.7]].map(([x,z,height],i)=>({kind:'tree',x,z,height,rotation:i*1.18})),
  ...[[7,-75],[12,-75],[21,-72],[27,-73],[38,-71],[43,-74],[48,-92],[51,-91],[32,-95],[37,-93],[13,-95],[17,-92],[-49,-93],[-47,-97],[46,-86],[47,-78],[4,-96]].map(([x,z],i)=>({kind:'shrub',x,z,height:.7+(i%4)*.12,rotation:i*2.1})),
  // Additional source Verdant Trail shrubs: edge planting only, not on
  // the basketball court, central park walk, villa entrances or roads.
  ...[[6,-71],[9,-71],[17,-70],[35,-70],[44,-70],[50,-72],
      [6,-89],[10,-91],[16,-91],[23,-94],[33,-90],[40,-89],[48,-88],
      [-49,-88],[-45,-91],[-40,-96],
      [-54,-50],[-54,-35],[-54,-20],[-54,0],[-54,22],[-54,43],
      [54,-50],[54,-35],[54,-20],[54,0],[54,22],[54,43]
     ].map(([x,z],i)=>({kind:'shrub',x,z,height:.6+(i%4)*.12,rotation:i*1.91})),
  ...[[46,-92],[45,-90],[38,-94],[12,-96],[51,-76],[-49,-96]].map(([x,z],i)=>({kind:'rock',x,z,height:.45+(i%3)*.15,rotation:i*1.4})),
 ],
 streets:['Wiśniowa','Bydgoska','Poznańska'],
 facilities:[
  {id:'casino',label:'Kasyno',x:-86,z:-42,width:40,depth:30},
  {id:'club',label:'Dyskoteka',x:-86,z:14,width:40,depth:30},
  {id:'police',label:'Komisariat',x:86,z:-42,width:40,depth:30},
  {id:'hospital',label:'Szpital',x:86,z:14,width:40,depth:30},
  {id:'boxing',label:'Sala bokserska',x:-86,z:64,width:40,depth:30},
  {id:'arena',label:'Hala walk · oktagon',x:86,z:64,width:40,depth:30},
 ],
 arenas:[
  {id:'boxing',type:'ring',center:[-86,63],bounds:{minX:-90,maxX:-82,minZ:59,maxZ:67},playerSpawn:[-88,63],opponentSpawn:[-84,63],entry:[-86,70],registration:[-99,75],payout:[-99,75],exits:[[-86,81],[-86,47]],spectators:[[-96,60],[-76,60]]},
  {id:'arena',type:'octagon',center:[86,63],radius:4.5,playerSpawn:[84,63],opponentSpawn:[88,63],entry:[86,70],registration:[73,75],payout:[73,75],exits:[[86,81],[86,47]],spectators:[[76,60],[96,60]]},
 ],
 missionSockets:[
  {id:'park-meeting',kind:'meeting',x:28,z:-83},
  {id:'court-challenge',kind:'challenge',x:-28,z:-72},
  {id:'casino-cashier',kind:'interaction',x:-95,z:-30},
  {id:'casino-vip',kind:'meeting',x:-99,z:-52},
  {id:'club-backstage',kind:'meeting',x:-99,z:4},
  {id:'police-desk',kind:'interaction',x:73,z:-29},
  {id:'hospital-reception',kind:'interaction',x:73,z:26},
  {id:'boxing-registration',kind:'fight-entry',arena:'boxing',x:-100,z:75},
  {id:'octagon-registration',kind:'fight-entry',arena:'arena',x:72,z:75},
  {id:'garage-delivery',kind:'delivery',x:-24,z:-27},
 ],
 points:[
  {id:'park',label:'Park Wiśniowy',x:20,z:-83,status:'explorable'},
  {id:'court',label:'Boisko',x:-28,z:-83,status:'explorable'},
  {id:'city',label:'Powrót do miasta',x:0,z:94,status:'integration-pending'},
  {id:'contact',label:'Dom kontaktowy',x:30,z:-32,status:'explorable'},
  {id:'garages',label:'Garaże',x:-37,z:-31,status:'planned'},
  {id:'villa',label:'Willa Wawera',x:0,z:-29,status:'explorable'},
  {id:'forest',label:'Droga do lasu',x:42,z:-56,status:'locked'},
 ],
};
