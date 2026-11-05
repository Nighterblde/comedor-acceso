export function leer(clave: string): string | null {
  try {
    return localStorage.getItem(clave);
  } catch {
    return null;
  }
}

export function guardar(clave: string, valor: string): void {
  try {
    localStorage.setItem(clave, valor);
  } catch {
    // Sin almacenamiento disponible: la sesión vive solo en esta pestaña.
  }
}

export function borrar(clave: string): void {
  try {
    localStorage.removeItem(clave);
  } catch {
    // Nada que borrar.
  }
}
