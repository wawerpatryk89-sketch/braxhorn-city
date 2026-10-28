// Both districts use the same keyboard actions and move menu.
export const INPUT_ACTIONS = { escape: 'menu', p: 'menu', ' ': 'jump', j: 'punch', k: 'kick', l: 'combo', u: 'kicks', e: 'throw', t: 'taunt', g: 'dance', r: 'warmup', v: 'cam', m: 'moves', q: 'iso-left', h: 'help', f: 'hook', x: 'dodge', o: 'act', i: 'special', b: 'shake', n: 'timefast', '1': 'view-model', '2': 'view-wire', '3': 'view-bones', enter: 'enter' };

export const MOVE_LIST = [
  ['Ataki', [['punch', 'Prosty + krzyżowy', 'J'], ['hook', 'Sierpowy', 'F'], ['kick', 'Kopnięcie MMA', 'K'], ['kicks', 'Seria kopnięć', 'U'], ['combo', 'Kombinacja kopnięć', 'L'], ['special', 'Cios specjalny postaci', 'I'], ['throw', 'Rzut przez bark (przy rywalu)', 'E']]],
  ['Obrona', [['block', 'Blok (przytrzymaj)', 'C'], ['dodge', 'Unik', 'X']]],
  ['Ruch', [['jump', 'Skok', 'Spacja'], ['act', 'Akcja: drzwi, podnieś, wejdź', 'O'], ['stepf', 'Krok bokserski w przód'], ['stepl', 'Długi wypad'], ['stepb', 'Krok w tył']]],
  ['Pokaz', [['taunt', 'Prowokacja', 'T'], ['warmup', 'Rozgrzewka', 'R'], ['guard', 'Garda'], ['dance', 'Taniec', 'G'], ['breakdance', 'Breakdance (Nyxa)']]],
  ['Świat', [['timefast', 'Przyspiesz dzień i noc', 'N'], ['cam', 'Kamera 3D / 2.5D', 'V']]],
];
