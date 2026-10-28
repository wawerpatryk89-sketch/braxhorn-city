// Story, heroes and bosses of BRAXHORN CITY.
export const HEROES = {
  brax: {
    key: 'brax', name: 'Brax', title: 'Właściciel BRAXHORN GYM',
    desc: 'Ciężka ręka, twarda głowa. Kolano i łokieć, których nikt nie chce poczuć dwa razy.',
    stats: { 'Siła': 9, 'Szybkość': 6, 'Obrona': 8 },
    specials: ['knee', 'elbow', 'bodyjab'], specialNames: ['Kolano', 'Łokieć', 'Seria na korpus'],
    hp: 120, dmg: 1.12, speed: 0.95,
  },
  nyxa: {
    key: 'nyxa', name: 'Nyxa', title: 'Mistrzyni kopnięć',
    desc: 'Najszybsza zawodniczka w mieście. Martelo, podwójne kopnięcie i breakdance na deser.',
    stats: { 'Siła': 7, 'Szybkość': 9, 'Obrona': 6 },
    specials: ['martelo', 'mma2', 'bodyjab'], specialNames: ['Martelo', 'Kopnięcie z obrotu', 'Seria na korpus'],
    hp: 100, dmg: 1.0, speed: 1.12,
  },
};

export const PROLOGUE = [
  { t: 'BRAXHORN CITY', s: 'Miasto, które nie śpi, bo boi się zamknąć oczy.' },
  { t: 'BRAXHORN GYM', s: 'Na starym mieście stoi siłownia Braxa i Nyxy. Ostatnie miejsce, gdzie walczy się uczciwie.' },
  { t: 'CZERWONY RÓG', s: 'Ulice przejął Asterion i jego gang. Rozprowadzają serum, które daje siłę… i zabiera duszę. Ci, którzy je biorą, nocą stają się Zarażonymi.' },
  { t: 'WEZWANIE', s: 'Dziś ktoś przybił do drzwi siłowni czerwony róg. To wezwanie: Krypta pod wiaduktem. Kto wygra w oktagonie, rządzi miastem.' },
];

export const CHAPTERS = {
  gym: { n: 'ROZDZIAŁ 1', t: 'SIŁOWNIA', s: 'Rozgrzewka przed burzą' },
  street: { n: 'ROZDZIAŁ 2', t: 'ULICA', s: 'Czerwony Róg wychodzi na miasto' },
  crypt: { n: 'ROZDZIAŁ 3', t: 'KRYPTA', s: 'Oktagon pod wiaduktem' },
};

// street bosses in order (x = where they wait on the street), then the crypt
export const BOSSES = [
  { id: 'pazur', model: 'tiger', name: 'Pazur', title: 'Egzekutorka Czerwonego Rogu', hp: 150, dmg: 0.9, x: 8, reward: 300,
    at: { pt: 'fight1_center', spawn: [-10, 85.5], r: 8, place: 'boisko na podwórku (brama naprzeciw siłowni)' },
    intro: 'Asterion przesyła pozdrowienia. Ja przekażę je osobiście.', outro: 'To… dopiero początek…',
    moves: ['jab', 'hook', 'kick', 'martelo', 'mma2', 'kicks'], speed: 1.05, aggro: 1.0 },
  { id: 'prega', model: 'tiger2', name: 'Pręga', title: 'Siostra Pazur', hp: 175, dmg: 1.0, x: 24, reward: 450,
    at: { x: -75, y: 0.15, z: 76, spawn: [-75, 81], r: 7, place: 'nabrzeże przy molo' },
    intro: 'Położyłeś moją siostrę. Teraz moja kolej.', introAlt: 'Pazur mówiła, że przyjdziesz. Zanim do niej dojdziesz, pogadasz ze mną.', introAltIf: 'pazur', outro: 'Krypta… cię zje…',
    moves: ['jab', 'kick', 'kicks', 'combo', 'martelo', 'mma2'], speed: 1.1, aggro: 1.15 },
  { id: 'roza', model: 'fant', name: 'Róża', title: 'Kiedyś trenowała w BRAXHORN GYM', hp: 200, dmg: 1.1, x: 40, reward: 600,
    at: { pt: 'club_dance', spawn: [-44, 52], r: 5.5, place: 'parkiet w dyskotece NOVA' },
    intro: 'Serum dało mi więcej niż twoja siłownia kiedykolwiek.', outro: 'Może… jeszcze nie jest za późno…',
    moves: ['jab', 'hook', 'knee', 'bodyjab', 'elbow', 'combo'], speed: 1.0, aggro: 1.2 },
  { id: 'kafar', model: 'musc', name: 'Kafar', title: 'Bramkarz Krypty', hp: 240, dmg: 1.2, x: 55, reward: 800, weapon: 'club',
    at: { pt: 'fight2_center', spawn: [-48.6, 48], r: 4.6, place: 'piwnica pod klubem NOVA (schody z zaplecza)' },
    intro: 'Bez zaproszenia nie wchodzisz. Zaproszeniem jest moja pałka.', outro: 'Wchodź… jeśli ci życie niemiłe.',
    moves: ['bat1', 'bat2', 'club4', 'bat3', 'clubKick'], speed: 0.9, aggro: 1.0 },
];
export const CRYPT_BOSSES = [
  { id: 'asterion', model: 'mino', name: 'Asterion', title: 'Szef gangu Czerwonego Rogu', hp: 320, dmg: 1.3, reward: 1500, size: 1.08,
    intro: 'Witaj w Krypcie. Tu kończą się legendy.', outro: 'Za mało… potrzebuję więcej serum…',
    moves: ['jab', 'hook', 'kick', 'kicks', 'combo', 'elbow', 'martelo'], speed: 1.0, aggro: 1.3 },
  { id: 'demon', model: 'demon', name: 'Rogaty', title: 'Prawdziwe źródło Czerwonego Rogu', hp: 420, dmg: 1.55, reward: 3000, size: 1.25,
    intro: 'Asterion wypił ostatnią fiolkę. Coś w nim pękło… Z cienia wyszedł Rogaty.', outro: '',
    moves: ['bat2', 'club4', 'bat1', 'bat3', 'martelo', 'kick'], speed: 1.05, aggro: 1.4, claws: true },
];
export const ZOMBIE = { model: 'zombie', name: 'Zarażony', hp: 46, dmg: 0.7, reward: 40, moves: ['jab', 'hook'] };

export const ENDING = [
  { t: 'ŚWIT', s: 'Nad Braxhorn City wstaje słońce. Rogaty leży w gruzach Krypty, a z nim cała moc Czerwonego Rogu.' },
  { t: 'BRAXHORN', s: 'Siłownia znów jest pełna. Miasto wie, kto tu rządzi.' },
];

export const HINTS = {
  gymStart: 'Rozgrzej się: wejdź na ring po schodkach i powalcz z partnerem. Kiedy będziesz gotów, wyjdź drzwiami WYJŚCIE.',
  door: 'Czerwony Róg czeka na ulicy. Idź na wschód, pod wiadukt.',
  street: 'Czterech bossów czeka w swoich miejscach: boisko, nabrzeże, klub NOVA i piwnica. Uważaj na auta!',
  night: 'Zapada noc. Zarażeni wychodzą na ulice…',
  day: 'Świta. Zarażeni chowają się w cieniu.',
  bat: 'Kolczasta pałka! Podnieś ją: przycisk AKCJA (klawisz O).',
  crypt: 'Drzwi do Krypty są otwarte. Podejdź i wejdź (AKCJA / O).',
  case: 'Walizka z pieniędzmi! Zanieś ją do siłowni (drzwi w zaułku), żeby zgarnąć nagrodę.',
};
