# Genera la hoja del ataque hacia arriba del reaper a partir del ataque básico (derecha).
# Separa el corte blanco del cuerpo, lo gira 90° hacia arriba y lo vuelve a pegar.
# La celda es más alta (201x200) para que quepa el corte sobre la cabeza; los pies quedan
# a la misma altura que en las demás hojas (37 px desde abajo -> pivote Y = 0.185).
#
# Uso: powershell -File generar_ataque_arriba.ps1 <ataque_basico.png> <salida.png>

param(
    [Parameter(Mandatory)][string]$Entrada,
    [Parameter(Mandatory)][string]$Salida
)

Add-Type -AssemblyName System.Drawing

$cellW = 201; $cellH = 140; $frames = 21; $cols = 8
$outH = 200
$extra = $outH - $cellH          # píxeles añadidos arriba
$cx = 97; $cy = 50               # centro del giro: delante de la cara

function Es-Claro($c) { $c.A -gt 0 -and ($c.R + $c.G + $c.B) / 3 -gt 150 }

$hoja = [Drawing.Bitmap]::new($Entrada)
$rows = [Math]::Ceiling($frames / $cols)
$out = [Drawing.Bitmap]::new($cellW * $cols, $outH * $rows, [Drawing.Imaging.PixelFormat]::Format32bppArgb)

for ($f = 0; $f -lt $frames; $f++) {
    $ox = ($f % $cols) * $cellW; $oy = [Math]::Floor($f / $cols) * $cellH
    $dx = ($f % $cols) * $cellW; $dy = [Math]::Floor($f / $cols) * $outH

    for ($y = 0; $y -lt $cellH; $y++) {
        # Lo más a la derecha que llega el cuerpo (oscuro) en esta fila.
        $maxOscuro = -1
        for ($x = 0; $x -lt $cellW; $x++) {
            $c = $hoja.GetPixel($ox + $x, $oy + $y)
            if ($c.A -gt 0 -and -not (Es-Claro $c)) { $maxOscuro = $x }
        }
        for ($x = 0; $x -lt $cellW; $x++) {
            $c = $hoja.GetPixel($ox + $x, $oy + $y)
            if ($c.A -eq 0) { continue }
            if ((Es-Claro $c) -and $x -gt $maxOscuro + 1) {
                # Corte: (x, y) -> giro antihorario alrededor de (cx, cy)
                $nx = $cx + ($y - $cy); $ny = $cy - ($x - $cx) + $extra
                if ($nx -ge 0 -and $nx -lt $cellW -and $ny -ge 0 -and $ny -lt $outH) {
                    $out.SetPixel($dx + $nx, $dy + $ny, $c)
                }
            } else {
                $out.SetPixel($dx + $x, $dy + $y + $extra, $c)
            }
        }
    }
}

$out.Save($Salida, [Drawing.Imaging.ImageFormat]::Png)
$hoja.Dispose(); $out.Dispose()
"Hoja generada: $Salida ($($cellW * $cols)x$($outH * $rows))"
