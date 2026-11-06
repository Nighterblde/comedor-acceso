// Prueba de carga de la validación de acceso (Tarea 15, RNF-01 y RNF-02).
//
// Escenario: 1,000 empleados con sesión ya iniciada, un punto de entrada,
// 50 validaciones por minuto durante 20 minutos y un pico de 150 por minuto
// durante 2 minutos. Una de cada diez validaciones es captura manual del chef.
//
// Debe correr contra una base de datos dedicada a la prueba: setup() crea el
// punto, la regla y el usuario encargado, y sincroniza al personal simulado.
// La preparación del entorno está en docs/aceptacion.md ("Prueba de carga").
//
// Variables (k6 run -e NOMBRE=valor):
//   API              URL base de la API (por omisión http://localhost:5080)
//   ADMIN_USUARIO    administrador sembrado en la base de prueba
//   ADMIN_CONTRASENA su contraseña
//   EMPLEADOS        número de empleados simulados (por omisión 1000)
//   PREFIJO          prefijo de matrícula (por omisión C); matrícula C0001…C1000
//   MINUTOS_SOSTENIDO / MINUTOS_PICO  duración de cada fase (20 y 2)

import http from 'k6/http';
import { check, fail } from 'k6';
import exec from 'k6/execution';
import { Counter } from 'k6/metrics';

const API = __ENV.API || 'http://localhost:5080';
const EMPLEADOS = parseInt(__ENV.EMPLEADOS || '1000', 10);
const PREFIJO = __ENV.PREFIJO || 'C';
const MIN_SOSTENIDO = parseInt(__ENV.MINUTOS_SOSTENIDO || '20', 10);
const MIN_PICO = parseInt(__ENV.MINUTOS_PICO || '2', 10);
const PUNTO = 'CARGA1';
const ENCARGADO = 'encargado-carga';
const CONTRASENA_ENCARGADO = 'Carga-Encargado-2026';

const autorizados = new Counter('autorizados');
const rechazados = new Counter('rechazados');

export const options = {
  setupTimeout: '10m',
  scenarios: {
    sostenido: {
      executor: 'constant-arrival-rate',
      rate: 50,
      timeUnit: '1m',
      duration: `${MIN_SOSTENIDO}m`,
      preAllocatedVUs: 10,
      maxVUs: 50,
      env: { FASE: 'sostenido' },
    },
    pico: {
      executor: 'constant-arrival-rate',
      rate: 150,
      timeUnit: '1m',
      duration: `${MIN_PICO}m`,
      startTime: `${MIN_SOSTENIDO}m`,
      preAllocatedVUs: 20,
      maxVUs: 100,
      env: { FASE: 'pico' },
    },
  },
  thresholds: {
    'http_req_duration{name:celular}': ['p(95)<2000'],
    'http_req_duration{name:manual}': ['p(95)<1000'],
    // Solo las validaciones; las peticiones de setup() no cuentan.
    'http_req_failed{scenario:sostenido}': ['rate<0.01'],
    'http_req_failed{scenario:pico}': ['rate<0.01'],
  },
};

function matricula(i) {
  return PREFIJO + String(i + 1).padStart(4, '0');
}

function json(cuerpo, token) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers.Authorization = `Bearer ${token}`;
  return { headers, tags: { name: 'setup' } };
}

function exigir(res, que) {
  if (res.status < 200 || res.status >= 300) {
    fail(`${que}: HTTP ${res.status} ${res.body}`);
  }
  return res;
}

function iniciarSistema(usuario, contrasena) {
  const res = exigir(
    http.post(`${API}/api/auth/login`, JSON.stringify({ usuario, contrasena }), json()),
    `inicio de sesión de ${usuario}`,
  );
  return res.json('token');
}

export function setup() {
  if (!__ENV.ADMIN_USUARIO || !__ENV.ADMIN_CONTRASENA) {
    fail('Faltan ADMIN_USUARIO y ADMIN_CONTRASENA');
  }
  const admin = iniciarSistema(__ENV.ADMIN_USUARIO, __ENV.ADMIN_CONTRASENA);
  const p = (ruta, cuerpo) =>
    exigir(http.put(`${API}${ruta}`, JSON.stringify(cuerpo), json(cuerpo, admin)), `PUT ${ruta}`);

  p('/api/admin/puntos', { id: PUNTO, nombre: 'Entrada de carga', activo: true });
  // Regla que cubre todo el día para que la prueba no dependa de la hora en que corre.
  p('/api/admin/reglas', {
    turno: 'Matutino', tiempoComida: 'Comida', inicio: '00:00', fin: '23:59', perteneceAJornadaAnterior: false,
  });
  p('/api/admin/usuarios', { usuario: ENCARGADO, rol: 'Encargado', activo: true, contrasena: CONTRASENA_ENCARGADO });
  exigir(http.post(`${API}/api/admin/sincronizacion`, null, json(null, admin)), 'sincronización');

  const qr = exigir(http.get(`${API}/api/puntos/${PUNTO}/qr`, json(null, admin)), 'QR del punto').json('token');
  const encargado = iniciarSistema(ENCARGADO, CONTRASENA_ENCARGADO);

  // Sesión previa de cada empleado (RNF-01 mide "con sesión ya iniciada").
  const tokens = [];
  for (let inicio = 0; inicio < EMPLEADOS; inicio += 50) {
    const lote = [];
    for (let i = inicio; i < Math.min(inicio + 50, EMPLEADOS); i++) {
      const m = matricula(i);
      lote.push(['POST', `${API}/api/empleados/sesion`, JSON.stringify({ id: m, contrasena: `carga-${m}` }), json()]);
    }
    http.batch(lote).forEach((res, j) => {
      exigir(res, `sesión de ${matricula(inicio + j)}`);
      tokens.push(res.json('token'));
    });
  }

  return { qr, encargado, tokens };
}

export default function (datos) {
  // Fase sostenida: cada iteración es un empleado distinto (1,000 consumos).
  // Fase pico: vuelven los primeros empleados; deben ser rechazados por "ya consumió".
  const n = exec.scenario.iterationInTest;
  const i = n % EMPLEADOS;
  const esManual = n % 10 === 0;

  const res = esManual
    ? http.post(`${API}/api/accesos/manual`, JSON.stringify({ matricula: matricula(i), puntoId: PUNTO }), {
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${datos.encargado}` },
        tags: { name: 'manual' },
      })
    : http.post(`${API}/api/accesos/celular`, JSON.stringify({ tokenQr: datos.qr }), {
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${datos.tokens[i]}` },
        tags: { name: 'celular' },
      });

  const estado = res.status === 200 ? res.json('estado') : null;
  const motivo = res.status === 200 ? res.json('motivo') : null;
  if (estado === 'Autorizado') autorizados.add(1);
  if (estado === 'Rechazado') rechazados.add(1);

  // constant-arrival-rate puede lanzar una iteración más que rate × duración; esa vuelta
  // repite a un empleado que ya comió y debe ser rechazada como en el pico.
  const primeraVez = __ENV.FASE === 'sostenido' && n < EMPLEADOS;
  const yaConsumio = estado === 'Rechazado' && motivo === 'Ya consumiste este tiempo de comida';
  check(res, {
    'responde 200': (r) => r.status === 200,
    'primera vez: autorizado': () => !primeraVez || estado === 'Autorizado',
    'repetido: rechazado por ya consumió': () => primeraVez || yaConsumio,
  });
}
