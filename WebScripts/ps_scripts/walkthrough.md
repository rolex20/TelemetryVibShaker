# Walkthrough: Soporte de Múltiples Procesos en `process_name`

Se implementó con éxito el soporte para definir múltiples procesos en la propiedad `process_name` de los archivos `action-per-process-boost*.json` (tanto para reglas principales como para dependencias), manteniendo 100% de compatibilidad hacia atrás con los archivos existentes.

---

## Cambios Realizados

### 1. Motor de Resolución en PowerShell

- **[`Set-IdealProcessor.ps1`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/Set-IdealProcessor.ps1)**:
  - Se rediseñó [`Get-TrimmedProcessNames`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/Set-IdealProcessor.ps1#L686) retirando la restricción rígida `[string]$processNames`.
  - Ahora soporta de forma polimórfica:
    - Cadenas simples: `"notepad"` $\rightarrow$ `@("notepad")`
    - Cadenas separadas por comas: `"notepad, TiWorker, CompatTelRunner"` $\rightarrow$ `@("notepad", "TiWorker", "CompatTelRunner")`
    - Arrays nativos JSON / PowerShell: `@("notepad", "TiWorker")` $\rightarrow$ `@("notepad", "TiWorker")`
    - Arrays mixtos con cadenas que contienen comas internas.
  - Sanitización automática: recorta espacios con `.Trim()`, elimina extensiones `.exe` accidentales (insensible a mayúsculas) para prevenir fallos en `Get-Process`, y descarta duplicados preservando el orden.
  - Garantiza un retorno fuertemente tipado `System.String[]`.

- **[`Set-GamePowerScheme.ps1`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/Set-GamePowerScheme.ps1)**:
  - Se actualizó [`Restore-GameBoost`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/Set-GamePowerScheme.ps1#L29) para que la búsqueda del bloque de acción utilice `Get-TrimmedProcessNames` con el operador `-contains` en lugar de la igualdad estricta `-eq`.
  - Se mejoró el formateo de diagnóstico en logs cuando una dependencia tiene múltiples procesos configurados.

---

### 2. Perfiles de Configuración JSON

- **[`action-per-process-boost4.json`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/action-per-process-boost4.json)**:
  - Se unificaron los bloques cruzados y duplicados de `TiWorker` y `CompatTelRunner` en una sola regla limpia usando la sintaxis de array JSON:
    ```json
    [
        {
            "comment": "Pesky windows background processes throttled to E-Cores and Idle while busy",
            "process_name": [
                "TiWorker",
                "CompatTelRunner"
            ],
            "parameters": {
                "process_affinity": "E-Cores",
                "process_priority": "Idle",
                "thread_ideal_processor": "DoNotChange",
                "thread_priority": "Idle",
                "thread_cpu_sets": "DoNotChange",
                "process_change_cpu_sets": false,
                "override_higher_priority": true,
                "dependencies": []
            }
        }
    ]
    ```

---

### 3. Documentación Oficial

- **[`README.md`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/README.md)**:
  - Se añadieron ejemplos completos de ambos estilos:
    - **Ejemplo 1**: Regla multivariada con cadena separada por comas (`"process_name": "FlightSimulator, aces, dcs"`) y dependencias con array (`"process_name": ["steamwebhelper", "steam"]`).
    - **Ejemplo 2**: Regla multivariada estructurada con array JSON (`"process_name": ["TiWorker", "CompatTelRunner"]`).
  - Se actualizó la tabla formal de especificación de parámetros y el catálogo del perfil `boost4`.

---

## Verificación y Pruebas Realizadas

### Nueva Suite Automatizada: [`ProcessNames.Tests.ps1`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/tests/ProcessNames.Tests.ps1)

Se ejecutaron **96 verificaciones unitarias** cubriendo:
1. Cadena de un solo proceso (compatibilidad hacia atrás).
2. Cadena separada por comas con espacios irregulares.
3. Arrays JSON y PowerShell.
4. Arrays mixtos conteniendo cadenas con comas.
5. Remoción de extensiones `.exe` (`notepad.exe`, `TiWorker.EXE`).
6. Deduplicación insensible a mayúsculas y minúsculas.
7. Manejo de valores nulos, vacíos y arrays vacíos.
8. Coincidencia de perfiles en `Restore-GameBoost` para todas las variantes.
9. Validación de parsing y resolución de nombres sobre los 4 perfiles reales (`boost1`, `boost2`, `boost3`, `boost4`).

### Pruebas de Regresión Completas

| Suite de Pruebas | Resultado | Aserciones Pasadas |
| :--- | :--- | :--- |
| **`ProcessNames.Tests.ps1`** | **PASS** | **96 passed**, 0 failed |
| **`Delayed-GameBoost.Tests.ps1`** | **PASS** | **13 passed**, 0 failed |
| **`Gaming-Programs.Tests.ps1`** | **PASS** | **9 passed**, 0 failed |
| **`Aux-Programs.Tests.ps1`** | **PASS** | **64 passed**, 0 failed |
| **Total General** | **100% EXIT 0** | **182 passed**, 0 failed |
