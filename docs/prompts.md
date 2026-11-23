### 1. Ejemplo de prompt parra corregir brecha, ajuste, etc

1. tengo el siguiente proyecto el cual consiste en una API hecha en .NET core, en los siguientes documentos puedes encontrar
mas información de su alcance, arquitectura, etc:

En @docs/arquitectura.md puedes encontrar la arquitectura, reglas de diseño, estándares de desarrollo, etc que se siguen
para su desarrollo

En @docs/requerimientos.md puedes encontrar los requerimientos del sistema, su alcance, lo que hace, etc

Para el desarrollo del proyecto, se uso el enfoque de spec driven design, en la carpeta @specs, pueden encontrar los specs
de los diferentes modulos desarrollados

En la siguiente carpeta @correciones, se documentan correciones, bugs, mejoras etc encontrados sobre su desarrollo, cuando
se desarrolla algo, siempre se redacta un documento para dejar evidencia de lo implementado

2. tengo la siguiente brecha, la cual quiero analizar si tiene sentido implementarla en el proyecto:

Ante un 429, la API envía el encabezado Retry-After con los segundos de espera, pero su CORS no lo expone (no tiene WithExposedHeaders). Retry-After no es un encabezado que el navegador deje leer desde otro origen, y el cuerpo del 429 no trae los segundos.

Desde otro origen (en desarrollo, http://localhost:5173) el frontend no puede leer N, así que no puede mostrar "Intente de nuevo en N segundos" ni deshabilitar el envío ese tiempo exacto (RUI-03).

Exponer el encabezado en el CORS de la API: .WithExposedHeaders("Retry-After"). Mientras tanto, el frontend muestra el detail del 429 ("Realizó demasiados intentos seguidos. Espere unos minutos e intente de nuevo") y deshabilita el envío 60 segundos (docs/arquitectura.md §3). En cuanto pueda leer el encabezado, aplica RUI-03 sin cambios.