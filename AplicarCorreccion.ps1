param(
    [Parameter(Mandatory = $false)]
    [string]$ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

function Normalize-Text {
    param([string]$Text)
    return $Text.Replace("`r`n", "`n").Replace("`r", "`n")
}

function Backup-File {
    param(
        [string]$FullPath,
        [string]$RelativePath,
        [string]$BackupRoot
    )

    $backupPath = Join-Path $BackupRoot $RelativePath
    $backupDirectory = Split-Path $backupPath -Parent

    if (-not (Test-Path $backupDirectory)) {
        New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
    }

    Copy-Item $FullPath $backupPath -Force
}

function Apply-Replacements {
    param(
        [string]$RelativePath,
        [array]$Replacements,
        [string]$BackupRoot
    )

    $fullPath = Join-Path $ProjectRoot $RelativePath

    if (-not (Test-Path $fullPath)) {
        throw "No se encontró el archivo: $fullPath"
    }

    $content = Normalize-Text ([System.IO.File]::ReadAllText($fullPath))
    $originalContent = $content

    foreach ($replacement in $Replacements) {
        $oldText = Normalize-Text $replacement.Old
        $newText = Normalize-Text $replacement.New

        if (-not $content.Contains($oldText)) {
            throw "No se encontró el bloque esperado en $RelativePath. El archivo puede pertenecer a otra versión."
        }

        $content = $content.Replace($oldText, $newText)
    }

    if ($content -eq $originalContent) {
        throw "No se aplicó ningún cambio en $RelativePath."
    }

    Backup-File `
        -FullPath $fullPath `
        -RelativePath $RelativePath `
        -BackupRoot $BackupRoot

    $utf8Bom = New-Object System.Text.UTF8Encoding($true)
    [System.IO.File]::WriteAllText(
        $fullPath,
        $content.Replace("`n", [Environment]::NewLine),
        $utf8Bom
    )

    Write-Host "Corregido: $RelativePath" -ForegroundColor Green
}

$requiredRootFile = Join-Path $ProjectRoot "CONATRADEC.sln"

if (-not (Test-Path $requiredRootFile)) {
    throw @"
La ruta indicada no parece ser la raíz del repositorio CONATRADEC.

Abra PowerShell en la carpeta que contiene:
CONATRADEC.sln

O ejecute:
.\AplicarCorreccion.ps1 -ProjectRoot "C:\Ruta\CONATRADEC"
"@
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupRoot = Join-Path $ProjectRoot "_backup_correccion_edicion_$timestamp"
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null

try {
    # ================================================================
    # 1. ResultadoAnalisisSueloViewModel
    #    - Reactiva Incluir/Excluir todos después de cargar Elementos.
    #    - No destruye Balance/Mixta al cambiar la selección.
    #    - Reconstruye los temporales con la selección que llegará
    #      realmente a Cálculos complementarios.
    # ================================================================
    Apply-Replacements `
        -RelativePath "CONATRADEC\ViewModels\ResultadoAnalisisSueloViewModel.cs" `
        -BackupRoot $backupRoot `
        -Replacements @(
            @{
                Old = @'
        private void NotificarElementosIncluidos()
        {
            OnPropertyChanged(
                nameof(TotalElementosIncluidos));

            OnPropertyChanged(
                nameof(TextoElementosIncluidos));
        }
'@
                New = @'
        private void NotificarElementosIncluidos()
        {
            OnPropertyChanged(
                nameof(TotalElementosIncluidos));

            OnPropertyChanged(
                nameof(TextoElementosIncluidos));

            /*
             * Los comandos se crearon cuando la colección todavía estaba
             * vacía. Al terminar de cargar los elementos se debe reevaluar
             * CanExecute; de lo contrario, Incluir todos y Excluir todos
             * pueden permanecer deshabilitados visualmente.
             */
            IncluirTodosElementosCommand
                .ChangeCanExecute();

            ExcluirTodosElementosCommand
                .ChangeCanExecute();
        }
'@
            },
            @{
                Old = @'
            await PrepararEdicionSegunSeleccionElementosAsync();

            /*
             * Se conserva el requerimiento anual completo para guardarlo,
             * mientras las pantallas complementarias reciben únicamente
             * los elementos que el usuario decidió incluir.
             */
            SeleccionElementosComplementariosService
                .GuardarRequerimientoCompleto(
                    Resultado,
                    RequestGuardarAnalisis
                        .IdentificadorAnalisisSuelo);

            AnalisisSueloCalculoDataResponse
                resultadoParaComplementarios =
                    SeleccionElementosComplementariosService
                        .CrearResultadoParaCalculosComplementarios(
                            Resultado);

            Dictionary<string, object>
'@
                New = @'
            /*
             * Se conserva el requerimiento anual completo para guardarlo,
             * mientras las pantallas complementarias reciben únicamente
             * los elementos que el usuario decidió incluir.
             */
            SeleccionElementosComplementariosService
                .GuardarRequerimientoCompleto(
                    Resultado,
                    RequestGuardarAnalisis
                        .IdentificadorAnalisisSuelo);

            AnalisisSueloCalculoDataResponse
                resultadoParaComplementarios =
                    SeleccionElementosComplementariosService
                        .CrearResultadoParaCalculosComplementarios(
                            Resultado);

            /*
             * En edición, Balance y Mixta ya fueron restaurados antes de
             * llegar a esta pantalla. Se vuelven a preparar utilizando
             * exactamente la lista filtrada que recibirá MultiCálculo,
             * sin eliminar el detalle persistido que permite recuperar
             * fuentes, cantidades y el vínculo entre ambos cálculos.
             */
            await PrepararEdicionSegunSeleccionElementosAsync(
                resultadoParaComplementarios);

            Dictionary<string, object>
'@
            },
            @{
                Old = @'
        private async Task
            PrepararEdicionSegunSeleccionElementosAsync()
        {
            if (!EsModoEdicion)
                return;

            HashSet<int> seleccionActual =
                Elementos
                    .Where(x =>
                        x.ElementoQuimicosId.HasValue &&
                        x.IncluirEnCalculosComplementarios)
                    .Select(x =>
                        x.ElementoQuimicosId!.Value)
                    .ToHashSet();

            if (seleccionActual.SetEquals(
                    elementosIncluidosInicialmente))
            {
                return;
            }

            AnalisisEdicionContexto? contexto =
                AnalisisEdicionService
                    .Instance
                    .ContextoActual;

            if (contexto == null)
                return;

            /*
             * El balance y la fertilización guardados fueron
             * calculados con otra selección de elementos.
             * No deben restaurarse como si siguieran vigentes.
             */
            if (contexto.Detalle.BalanceNutricional != null)
            {
                contexto.Detalle.BalanceNutricional =
                    null;

                await CalculoAnalisisTemporalService
                    .Instance
                    .ReiniciarCalculoAsync(
                        TipoCalculoTemporal
                            .BalanceFormula,
                        "La selección de elementos cambió. Debe recalcular el balance.");
            }

            if (contexto.Detalle.FertilizacionMixta != null)
            {
                contexto.Detalle.FertilizacionMixta =
                    null;

                await CalculoAnalisisTemporalService
                    .Instance
                    .ReiniciarCalculoAsync(
                        TipoCalculoTemporal
                            .FertilizacionMixta,
                        "La selección de elementos cambió. Debe recalcular la fertilización mixta.");
            }

            AnalisisEdicionService
                .Instance
                .RestauracionUiRealizada = false;

            MensajeSeleccionCalculo =
                "La selección de elementos cambió. Balance y Fertilización mixta deberán recalcularse antes de actualizar.";
        }
'@
                New = @'
        private async Task
            PrepararEdicionSegunSeleccionElementosAsync(
                AnalisisSueloCalculoDataResponse
                    resultadoParaComplementarios)
        {
            if (!EsModoEdicion ||
                RequestGuardarAnalisis == null)
            {
                return;
            }

            AnalisisEdicionContexto? contexto =
                AnalisisEdicionService
                    .Instance
                    .ContextoActual;

            if (contexto == null)
                return;

            HashSet<int> seleccionActual =
                Elementos
                    .Where(x =>
                        x.ElementoQuimicosId.HasValue &&
                        x.IncluirEnCalculosComplementarios)
                    .Select(x =>
                        x.ElementoQuimicosId!.Value)
                    .ToHashSet();

            bool seleccionCambio =
                !seleccionActual.SetEquals(
                    elementosIncluidosInicialmente);

            int plantas =
                CantidadPlantas is > 0
                    ? CantidadPlantas.Value
                    : contexto.CantidadPlantas;

            bool requerimientoCambio =
                AnalisisEdicionService
                    .Instance
                    .CambioRequerimiento(
                        RequestGuardarAnalisis);

            /*
             * El detalle persistido nunca se pone en null. Ese detalle es
             * precisamente el respaldo necesario para recuperar las fuentes
             * del Balance y las cantidades de Fertilización mixta.
             *
             * El servicio reconstruye los cálculos temporales con los
             * elementos actualmente incluidos. Cuando cambió el análisis o
             * la selección, recalcula los módulos conservando sus elecciones.
             */
            await AnalisisEdicionService
                .Instance
                .RestaurarTemporalAsync(
                    resultadoParaComplementarios,
                    RequestGuardarAnalisis,
                    plantas,
                    requerimientoCambio ||
                    seleccionCambio,
                    incluirBalance:
                        CalcularBalanceFormula,
                    incluirEnmienda:
                        CalcularEnmiendaCalcarea,
                    incluirMixta:
                        CalcularFertilizacionMixta);

            if (seleccionCambio)
            {
                MensajeSeleccionCalculo =
                    "La selección de elementos cambió. Se conservarán las fuentes compatibles y los cálculos seleccionados se actualizarán con la nueva selección.";
            }
        }
'@
            }
        )

    # ================================================================
    # 2. AnalisisEdicionService
    #    - Permite restaurar únicamente los módulos seleccionados.
    #    - Filtra el request del Balance según los elementos actuales.
    #    - Conserva el detalle guardado como fuente de restauración.
    # ================================================================
    Apply-Replacements `
        -RelativePath "CONATRADEC\Services\AnalisisEdicionService.cs" `
        -BackupRoot $backupRoot `
        -Replacements @(
            @{
                Old = @'
        public async Task RestaurarTemporalAsync(
            AnalisisSueloCalculoDataResponse resultado,
            AnalisisSueloGuardarCalculoRequest request,
            int plantas,
            bool requerimientoCambio)
'@
                New = @'
        public async Task RestaurarTemporalAsync(
            AnalisisSueloCalculoDataResponse resultado,
            AnalisisSueloGuardarCalculoRequest request,
            int plantas,
            bool requerimientoCambio,
            bool incluirBalance = true,
            bool incluirEnmienda = true,
            bool incluirMixta = true)
'@
            },
            @{
                Old = @'
            if (contexto.TieneBalance)
            {
                await RestaurarBalanceAsync(
                    contexto,
                    resultado,
                    plantas,
                    requerimientoCambio || CambioBalance(request, plantas));
            }

            if (contexto.TieneEnmienda)
            {
                await RestaurarEnmiendaAsync(
                    contexto,
                    request,
                    plantas,
                    CambioEnmienda(request, plantas));
            }

            if (contexto.TieneMixta)
            {
                await RestaurarMixtaAsync(
                    contexto,
                    resultado,
                    requerimientoCambio);
            }
'@
                New = @'
            if (incluirBalance &&
                contexto.TieneBalance)
            {
                await RestaurarBalanceAsync(
                    contexto,
                    resultado,
                    plantas,
                    requerimientoCambio ||
                    CambioBalance(
                        request,
                        plantas));
            }

            if (incluirEnmienda &&
                contexto.TieneEnmienda)
            {
                await RestaurarEnmiendaAsync(
                    contexto,
                    request,
                    plantas,
                    CambioEnmienda(
                        request,
                        plantas));
            }

            if (incluirMixta &&
                contexto.TieneMixta)
            {
                await RestaurarMixtaAsync(
                    contexto,
                    resultado,
                    requerimientoCambio);
            }
'@
            },
            @{
                Old = @'
            BalanceNutricionalRequest request =
                ConstruirRequestBalance(guardado, resultadoAnual, plantas);

            BalanceNutricionalResponse resultado;
'@
                New = @'
            BalanceNutricionalRequest request =
                ConstruirRequestBalance(
                    guardado,
                    resultadoAnual,
                    plantas);

            /*
             * Puede ocurrir cuando el usuario dejó seleccionado únicamente
             * un elemento que no participaba en el Balance guardado.
             * En ese caso no existe una fuente anterior que restaurar y la
             * pantalla debe quedar lista para una selección nueva, no fallar.
             */
            if (request.Items.Count == 0)
            {
                await CalculoAnalisisTemporalService
                    .Instance
                    .ReiniciarCalculoAsync(
                        TipoCalculoTemporal
                            .BalanceFormula,
                        "La selección actual no tiene fuentes guardadas. Seleccione una fuente para calcular el balance.");

                return;
            }

            BalanceNutricionalResponse resultado;
'@
            },
            @{
                Old = @'
            Dictionary<int, decimal> requerimientos =
                resultadoAnual.Elementos
                    .Where(x => x.ElementoQuimicosId.HasValue)
                    .GroupBy(x => x.ElementoQuimicosId!.Value)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First().RequerimientoCalculado ?? 0);

            foreach (AnalisisGuardadoFormulaDetalle detalle
                     in guardado.Detalles)
            {
'@
                New = @'
            Dictionary<int, decimal> requerimientos =
                resultadoAnual.Elementos
                    .Where(x =>
                        x.ElementoQuimicosId.HasValue)
                    .GroupBy(x =>
                        x.ElementoQuimicosId!.Value)
                    .ToDictionary(
                        x => x.Key,
                        x =>
                            x.First()
                                .RequerimientoCalculado ??
                            0);

            HashSet<int> elementosSeleccionados =
                requerimientos
                    .Keys
                    .ToHashSet();

            /*
             * Solo se restauran fuentes correspondientes a elementos que
             * continúan incluidos. El resto permanece en el requerimiento
             * anual completo, pero no participa en Balance ni Mixta.
             */
            foreach (
                AnalisisGuardadoFormulaDetalle detalle
                in guardado.Detalles.Where(x =>
                    elementosSeleccionados.Contains(
                        x.ElementoQuimicosId)))
            {
'@
            }
        )

    # ================================================================
    # 3. MultiCalculoViewModel
    #    - Evita reemplazar los temporales recién restaurados en edición.
    # ================================================================
    Apply-Replacements `
        -RelativePath "CONATRADEC\ViewModels\MultiCalculoViewModel.cs" `
        -BackupRoot $backupRoot `
        -Replacements @(
            @{
                Old = @'
            await CalculoAnalisisTemporalService.Instance
                .IniciarNuevoCalculoAsync(
                    ResultadoCalculo,
                    RequestGuardarAnalisis);

            InicializarTabs();
'@
                New = @'
            /*
             * En edición, ResultadoAnalisisSueloViewModel ya reconstruyó
             * Requerimiento, Balance, Enmienda y Mixta usando la selección
             * actual. Volver a iniciar aquí el cálculo cambia CalculoKey por
             * la lista filtrada y puede borrar esas secciones temporales.
             */
            if (!EsModoEdicion)
            {
                await CalculoAnalisisTemporalService
                    .Instance
                    .IniciarNuevoCalculoAsync(
                        ResultadoCalculo,
                        RequestGuardarAnalisis);
            }

            InicializarTabs();
'@
            }
        )

    # ================================================================
    # 4. RestaurarCalculosEdicionUiService
    #    - El respaldo usa el resultado actual, no el resultado original
    #      anterior a la edición.
    # ================================================================
    Apply-Replacements `
        -RelativePath "CONATRADEC\Services\RestaurarCalculosEdicionUiService.cs" `
        -BackupRoot $backupRoot `
        -Replacements @(
            @{
                Old = @'
                await AsegurarTemporalesGuardadosAsync(contexto);
'@
                New = @'
                await AsegurarTemporalesGuardadosAsync(
                    contexto,
                    viewModel);
'@
            },
            @{
                Old = @'
        private static async Task
            AsegurarTemporalesGuardadosAsync(
                AnalisisEdicionContexto contexto)
        {
            bool balanceFaltante =
                contexto.TieneBalance &&
                !TieneBalanceTemporal();

            bool enmiendaFaltante =
                contexto.TieneEnmienda &&
                !TieneEnmiendaTemporal();

            bool mixtaFaltante =
                contexto.TieneMixta &&
                !TieneMixtaTemporal();

            if (!balanceFaltante &&
                !enmiendaFaltante &&
                !mixtaFaltante)
            {
                return;
            }

            await AnalisisEdicionService
                .Instance
                .RestaurarTemporalAsync(
                    contexto.ResultadoOriginal,
                    contexto.RequestActual,
                    contexto.CantidadPlantas,
                    false);
        }
'@
                New = @'
        private static async Task
            AsegurarTemporalesGuardadosAsync(
                AnalisisEdicionContexto contexto,
                MultiCalculoViewModel viewModel)
        {
            bool balanceFaltante =
                viewModel.MostrarBalanceFormula &&
                contexto.TieneBalance &&
                !TieneBalanceTemporal();

            bool enmiendaFaltante =
                viewModel.MostrarEnmiendaCalcarea &&
                contexto.TieneEnmienda &&
                !TieneEnmiendaTemporal();

            bool mixtaFaltante =
                viewModel.MostrarFertilizacionMixta &&
                contexto.TieneMixta &&
                !TieneMixtaTemporal();

            if (!balanceFaltante &&
                !enmiendaFaltante &&
                !mixtaFaltante)
            {
                return;
            }

            AnalisisSueloCalculoDataResponse
                resultadoActual =
                    viewModel.ResultadoCalculo ??
                    contexto.ResultadoOriginal;

            AnalisisSueloGuardarCalculoRequest
                requestActual =
                    viewModel.RequestGuardarAnalisis ??
                    contexto.RequestActual;

            int plantas =
                viewModel.CantidadPlantas is > 0
                    ? viewModel.CantidadPlantas.Value
                    : contexto.CantidadPlantas;

            bool requerimientoCambio =
                AnalisisEdicionService
                    .Instance
                    .CambioRequerimiento(
                        requestActual);

            /*
             * El respaldo debe reconstruirse con el resultado que está
             * mostrando MultiCálculo. Usar ResultadoOriginal reintroducía
             * valores anteriores —por ejemplo 506 en lugar de 442.20— y
             * hacía que la lista de elementos no coincidiera con la UI.
             */
            await AnalisisEdicionService
                .Instance
                .RestaurarTemporalAsync(
                    resultadoActual,
                    requestActual,
                    plantas,
                    requerimientoCambio,
                    incluirBalance:
                        viewModel.MostrarBalanceFormula,
                    incluirEnmienda:
                        viewModel.MostrarEnmiendaCalcarea,
                    incluirMixta:
                        viewModel.MostrarFertilizacionMixta);
        }
'@
            }
        )

    # ================================================================
    # 5. MultiCalculoPage
    #    - Captura el estado de Mixta después de recibir los parámetros.
    #    - Reinicia la captura cuando cambia el resultado/navegación.
    # ================================================================
    Apply-Replacements `
        -RelativePath "CONATRADEC\Views\MultiCalculoPage.xaml.cs" `
        -BackupRoot $backupRoot `
        -Replacements @(
            @{
                Old = @'
        private bool estadoInicialMixtaCapturado;
        private bool mixtaSeleccionadaOriginalmente;
        private bool mixtaActivadaPorComplemento;
'@
                New = @'
        private bool estadoInicialMixtaCapturado;
        private bool mixtaSeleccionadaOriginalmente;
        private bool mixtaActivadaPorComplemento;

        private int? analisisCapturadoId;
        private string identificadorCapturado =
            string.Empty;

        private AnalisisSueloCalculoDataResponse?
            resultadoCapturado;
'@
            },
            @{
                Old = @'
            CapturarSeleccionOriginalMixta();

            /*
             * MultiCalculoPage es un ShellContent y MAUI conserva
'@
                New = @'
            /*
             * MultiCalculoPage es un ShellContent y MAUI conserva
'@
            },
            @{
                Old = @'
            await EsperarInicializacionActualAsync();

            await RestaurarCalculosEdicionUiService
'@
                New = @'
            await EsperarInicializacionActualAsync();

            /*
             * ApplyQueryAttributes es async void. Por eso la selección
             * original de Mixta debe capturarse después de que el ViewModel
             * haya recibido el análisis y las pestañas seleccionadas.
             */
            PrepararCapturaSeleccionOriginalMixta();

            await RestaurarCalculosEdicionUiService
'@
            },
            @{
                Old = @'
        private void CapturarSeleccionOriginalMixta()
        {
'@
                New = @'
        private void
            PrepararCapturaSeleccionOriginalMixta()
        {
            int? idActual =
                viewModel
                    .AnalisisSueloCalculoIdEdicion;

            string identificadorActual =
                viewModel
                    .NombreAnalisisSuelo;

            AnalisisSueloCalculoDataResponse?
                resultadoActual =
                    viewModel.ResultadoCalculo;

            bool correspondeMismaNavegacion =
                estadoInicialMixtaCapturado &&
                analisisCapturadoId == idActual &&
                string.Equals(
                    identificadorCapturado,
                    identificadorActual,
                    StringComparison.Ordinal) &&
                ReferenceEquals(
                    resultadoCapturado,
                    resultadoActual);

            if (correspondeMismaNavegacion)
                return;

            estadoInicialMixtaCapturado = false;
            mixtaSeleccionadaOriginalmente = false;
            mixtaActivadaPorComplemento = false;

            analisisCapturadoId = idActual;
            identificadorCapturado =
                identificadorActual;
            resultadoCapturado =
                resultadoActual;

            CapturarSeleccionOriginalMixta();
        }

        private void CapturarSeleccionOriginalMixta()
        {
'@
            }
        )

    Write-Host ""
    Write-Host "Corrección aplicada correctamente." -ForegroundColor Cyan
    Write-Host "Respaldo creado en:" -ForegroundColor Cyan
    Write-Host $backupRoot -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Ahora limpie bin y obj, recompilé y pruebe la edición con conexión." -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host "No se pudo completar la corrección:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Los archivos modificados antes del error tienen respaldo en:" -ForegroundColor Yellow
    Write-Host $backupRoot -ForegroundColor Yellow
    exit 1
}
