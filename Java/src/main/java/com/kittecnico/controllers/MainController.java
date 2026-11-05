package com.kittecnico.controllers;

import atlantafx.base.theme.Styles;
import com.kittecnico.models.AppItem;
import com.kittecnico.services.AppDataService;
import com.kittecnico.services.InstallationService;
import com.kittecnico.services.PowerShellService;
import javafx.application.Platform;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.collections.transformation.FilteredList;
import javafx.concurrent.Task;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.geometry.Insets;
import javafx.geometry.Pos;
import javafx.scene.Node;
import javafx.scene.Scene;
import javafx.scene.control.*;
import javafx.scene.image.Image;
import javafx.scene.image.ImageView;
import javafx.scene.layout.*;
import javafx.scene.paint.Color;
import javafx.stage.Stage;
import org.controlsfx.control.Notifications;
import org.kordamp.ikonli.javafx.FontIcon;
import org.kordamp.ikonli.material2.Material2OutlinedAL;
import org.kordamp.ikonli.material2.Material2OutlinedMZ;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.net.URL;
import java.time.LocalTime;
import java.time.format.DateTimeFormatter;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.stream.Collectors;

/**
 * Controller principal — MainView.fxml
 * Gerencia a lista de aplicativos, instalações, filtros e o painel de log.
 */
public class MainController implements Initializable {

    private static final Logger log = LoggerFactory.getLogger(MainController.class);
    private static final DateTimeFormatter TIME_FMT = DateTimeFormatter.ofPattern("HH:mm:ss");

    // ── FXML Injectados ─────────────────────────────────────────────────────
    @FXML private ListView<String>  lstCategorias;
    @FXML private FlowPane          flowApps;
    @FXML private TextField         txtBusca;
    @FXML private Button            btnInstalarTudo;
    @FXML private Button            btnInstalarSelecionados;
    @FXML private Button            btnAtualizarTudo;
    @FXML private Button            btnCancelar;
    @FXML private Button            btnVerificarInstalados;
    @FXML private ProgressBar       barProgresso;
    @FXML private Label             lblStatus;
    @FXML private Label             lblContagem;
    @FXML private Label             lblAdmin;
    @FXML private Label             lblInternet;
    @FXML private Label             lblWinget;
    @FXML private VBox              panelLog;
    @FXML private ScrollPane        scrollLog;
    @FXML private HBox              titleBar;

    private double xOffset = 0;
    private double yOffset = 0;

    // ── Serviços ─────────────────────────────────────────────────────────────
    private final PowerShellService    psService     = new PowerShellService();
    private final AppDataService       dataService   = new AppDataService();
    private final InstallationService  installer     = new InstallationService(psService);

    // ── Estado e Caches ──────────────────────────────────────────────────────
    private final ObservableList<AppItem> allApps      = FXCollections.observableArrayList();
    private final FilteredList<AppItem>   filteredApps = new FilteredList<>(allApps, p -> true);
    private final Map<String, Image>      imageCache   = new ConcurrentHashMap<>();
    private final Map<AppItem, VBox>      cardCache    = new IdentityHashMap<>();
    private final AtomicBoolean           isBusy       = new AtomicBoolean(false);
    private String categoriaAtual = "Todos";
    private String buscaAtual     = "";

    @Override
    public void initialize(URL url, ResourceBundle rb) {
        setupSearch();
        setupButtons();
        loadApps();
        setupCategories();
        applyFilter();
        checkSystemAsync();

        if (titleBar != null) {
            titleBar.setOnMousePressed(event -> {
                xOffset = event.getSceneX();
                yOffset = event.getSceneY();
            });
            titleBar.setOnMouseDragged(event -> {
                Stage stage = (Stage) titleBar.getScene().getWindow();
                stage.setX(event.getScreenX() - xOffset);
                stage.setY(event.getScreenY() - yOffset);
            });
        }
    }

    // ── Setup ────────────────────────────────────────────────────────────────

    private void setupCategories() {
        Set<String> cats = new TreeSet<>();
        for (AppItem app : allApps) {
            if (app.getCategoria() != null && !app.getCategoria().isBlank()) {
                cats.add(app.getCategoria());
            }
        }
        ObservableList<String> list = FXCollections.observableArrayList();
        list.add("Todos");
        list.addAll(cats);

        lstCategorias.setItems(list);
        lstCategorias.getSelectionModel().selectFirst();
        lstCategorias.getSelectionModel().selectedItemProperty().addListener((obs, old, newVal) -> {
            if (newVal != null) {
                categoriaAtual = newVal;
                applyFilter();
            }
        });
    }

    private final javafx.animation.PauseTransition searchDebounce =
        new javafx.animation.PauseTransition(javafx.util.Duration.millis(220));

    private void setupSearch() {
        searchDebounce.setOnFinished(e -> applyFilter());
        txtBusca.textProperty().addListener((obs, old, newVal) -> {
            buscaAtual = newVal == null ? "" : newVal.trim().toLowerCase();
            searchDebounce.playFromStart();
        });
    }

    private void setupButtons() {
        btnCancelar.setDisable(true);
        btnInstalarTudo.setOnAction(e -> installAll());
        btnInstalarSelecionados.setOnAction(e -> installSelected());
        btnAtualizarTudo.setOnAction(e -> updateAll());
        btnVerificarInstalados.setOnAction(e -> checkInstalled());
        btnCancelar.setOnAction(e -> cancelBusy());
    }

    private void loadApps() {
        List<AppItem> loaded = dataService.loadApps();
        cardCache.clear();
        allApps.setAll(loaded);
        lblContagem.setText(loaded.size() + " / " + loaded.size() + " apps");
        addLog("🚀 Kit Técnico Pro iniciado! " + loaded.size() + " apps carregados.", "success");
    }

    // ── Filtro ───────────────────────────────────────────────────────────────

    private void applyFilter() {
        filteredApps.setPredicate(app -> {
            String cat  = Optional.ofNullable(app.getCategoria()).orElse("");
            String nome = Optional.ofNullable(app.getNome()).orElse("").toLowerCase();
            String desc = Optional.ofNullable(app.getDescricao()).orElse("").toLowerCase();
            String wid  = Optional.ofNullable(app.getWingetId()).orElse("").toLowerCase();

            boolean catOk = "Todos".equals(categoriaAtual) || cat.equals(categoriaAtual);
            boolean busOk = buscaAtual.isEmpty()
                || nome.contains(buscaAtual)
                || desc.contains(buscaAtual)
                || cat.toLowerCase().contains(buscaAtual)
                || wid.contains(buscaAtual);
            return catOk && busOk;
        });

        lblContagem.setText(filteredApps.size() + " / " + allApps.size() + " apps");
        renderCards(filteredApps);
    }

    @FXML
    public void clearLog() {
        panelLog.getChildren().clear();
    }

    // ── Renderização de Cards (com Cache de Nós e Imagens) ───────────────────

    private void renderCards(List<AppItem> apps) {
        List<Node> cards = new ArrayList<>(apps.size());
        for (AppItem app : apps) {
            cards.add(cardCache.computeIfAbsent(app, this::buildCard));
        }
        flowApps.getChildren().setAll(cards);
    }

    /**
     * Constrói um Card moderno para cada AppItem (instanciado apenas 1 vez por AppItem).
     */
    private VBox buildCard(AppItem app) {
        VBox card = new VBox(8);
        card.setPrefWidth(295);
        card.setPrefHeight(140);
        card.setPadding(new Insets(14));
        card.getStyleClass().addAll("app-card");

        // ── Linha Superior: Ícone + Nome + Status ────────────────────────────
        HBox header = new HBox(10);
        header.setAlignment(Pos.CENTER_LEFT);

        Node iconeNode;
        String iconUrl = app.getIconUrl();
        if (iconUrl != null && !iconUrl.isBlank()) {
            Image image = imageCache.computeIfAbsent(iconUrl, u -> new Image(u, 32, 32, true, true, true));
            ImageView img = new ImageView(image);
            img.setFitWidth(32);
            img.setFitHeight(32);
            iconeNode = img;
        } else {
            FontIcon icone = new FontIcon(app.getResolvedIconLiteral());
            icone.setIconSize(32);
            icone.setIconColor(Color.web("#38bdf8"));
            iconeNode = icone;
        }

        // Nome e descrição
        VBox nameBox = new VBox(2);
        HBox.setHgrow(nameBox, Priority.ALWAYS);
        Label lblNome = new Label(app.getNome());
        lblNome.getStyleClass().add("app-name");
        Label lblDesc = new Label(app.getDescricao());
        lblDesc.getStyleClass().add("app-desc");
        lblDesc.setMaxWidth(175);
        nameBox.getChildren().addAll(lblNome, lblDesc);

        // Badge de status
        Label badge = new Label(app.getStatus());
        badge.getStyleClass().addAll("status-badge", app.getStatusCssClass());
        app.statusProperty().addListener((obs, o, n) -> {
            badge.setText(n);
            badge.getStyleClass().removeIf(c -> c.startsWith("status-"));
            badge.getStyleClass().addAll("status-badge", app.getStatusCssClass());
        });

        header.getChildren().addAll(iconeNode, nameBox, badge);

        // ── Linha de Tags ────────────────────────────────────────────────────
        HBox tags = new HBox(6);
        tags.setAlignment(Pos.CENTER_LEFT);
        String wid = app.getWingetId();
        tags.getChildren().addAll(
            tag(Optional.ofNullable(app.getCategoria()).orElse("Geral")),
            tag(Optional.ofNullable(app.getTipo()).orElse("winget").toUpperCase()),
            tag((wid == null || wid.isBlank()) ? "URL" : wid)
        );

        // ── Barra de Progresso ───────────────────────────────────────────────
        ProgressBar pb = new ProgressBar(0);
        pb.setPrefWidth(Double.MAX_VALUE);
        pb.setVisible(false);
        app.emProgressoProperty().addListener((obs, o, n) -> pb.setVisible(n));
        app.progressoProperty().addListener((obs, o, n) -> pb.setProgress(n.doubleValue()));

        // ── Linha de Botões ───────────────────────────────────────────────────
        HBox buttons = new HBox(6);
        buttons.setAlignment(Pos.CENTER_RIGHT);

        CheckBox chk = new CheckBox();
        chk.selectedProperty().bindBidirectional(app.selecionadoProperty());
        HBox.setHgrow(chk, Priority.ALWAYS);

        Button btnInstalar = new Button("", new FontIcon(Material2OutlinedMZ.SYSTEM_UPDATE));
        btnInstalar.getStyleClass().addAll(Styles.BUTTON_CIRCLE, Styles.SUCCESS);
        btnInstalar.setTooltip(new Tooltip("Instalar " + app.getNome()));
        btnInstalar.setOnAction(e -> installOne(app));
        app.emProgressoProperty().addListener((obs, o, n) -> btnInstalar.setDisable(n || isBusy.get()));

        Button btnOffline = new Button("", new FontIcon(Material2OutlinedMZ.SAVE_ALT));
        btnOffline.getStyleClass().addAll(Styles.BUTTON_CIRCLE, Styles.ACCENT);
        btnOffline.setTooltip(new Tooltip("Baixar instalador offline"));
        btnOffline.setOnAction(e -> downloadOffline(app));

        Button btnRemover = new Button("", new FontIcon(Material2OutlinedAL.DELETE_OUTLINE));
        btnRemover.getStyleClass().addAll(Styles.BUTTON_CIRCLE, Styles.DANGER);
        btnRemover.setTooltip(new Tooltip("Remover da lista"));
        btnRemover.setOnAction(e -> removeApp(app));

        buttons.getChildren().addAll(chk, btnInstalar, btnOffline, btnRemover);
        card.getChildren().addAll(header, tags, pb, buttons);

        card.setOnMouseEntered(e -> card.getStyleClass().add("app-card-hover"));
        card.setOnMouseExited(e -> card.getStyleClass().remove("app-card-hover"));

        return card;
    }

    private Label tag(String text) {
        Label l = new Label(text);
        l.getStyleClass().add("app-tag");
        return l;
    }

    // ── Ações de Instalação ──────────────────────────────────────────────────

    private void installOne(AppItem app) {
        if (isBusy.get()) return;
        setBusy(true);
        addLog("▶ Instalando: " + app.getNome(), "info");
        app.setEmProgresso(true);
        app.setStatus("⏳ Instalando...");

        installer.installAsync(app, msg -> Platform.runLater(() -> addLog(msg, "info")))
            .thenAccept(success -> Platform.runLater(() -> {
                app.setEmProgresso(false);
                app.setStatus(success ? "✅ Instalado" : "❌ Falhou");
                addLog(success
                    ? "✅ " + app.getNome() + " instalado com sucesso!"
                    : "❌ Falha ao instalar " + app.getNome(), success ? "success" : "error");
                dataService.saveApps(allApps);
                setBusy(false);
                showNotification(app.getNome(), success);
            }));
    }

    private void installAll() {
        List<AppItem> list = new ArrayList<>(filteredApps);
        if (list.isEmpty()) return;
        runInstallBatch(list, "Instalando filtrados...");
    }

    private void installSelected() {
        List<AppItem> selected = allApps.stream()
            .filter(AppItem::isSelecionado).collect(Collectors.toList());
        if (selected.isEmpty()) {
            addLog("⚠️ Nenhum app selecionado.", "warning");
            return;
        }
        runInstallBatch(selected, "Instalando selecionados...");
    }

    private void runInstallBatch(List<AppItem> apps, String msg) {
        if (isBusy.get()) return;
        setBusy(true);
        addLog("━━━ " + msg + " (" + apps.size() + " apps) ━━━", "info");

        Task<Void> task = new Task<>() {
            @Override
            protected Void call() {
                int total = apps.size();
                for (int i = 0; i < total; i++) {
                    if (!isBusy.get()) break;
                    AppItem app = apps.get(i);
                    final int idx = i;

                    Platform.runLater(() -> {
                        barProgresso.setProgress((double) idx / total);
                        lblStatus.setText("Instalando " + app.getNome() + "...");
                        app.setEmProgresso(true);
                        app.setStatus("⏳ Instalando...");
                    });

                    try {
                        boolean ok = installer.installAsync(
                            app, msg2 -> Platform.runLater(() -> addLog(msg2, "info"))
                        ).get();

                        Platform.runLater(() -> {
                            app.setEmProgresso(false);
                            app.setStatus(ok ? "✅ Instalado" : "❌ Falhou");
                        });
                    } catch (Exception e) {
                        Platform.runLater(() -> {
                            app.setEmProgresso(false);
                            app.setStatus("❌ Erro");
                        });
                    }
                }
                return null;
            }

            @Override
            protected void succeeded() {
                setBusy(false);
                barProgresso.setProgress(1.0);
                addLog("✅ Batch finalizado!", "success");
                dataService.saveApps(allApps);
            }
        };

        Thread t = new Thread(task, "ktp-batch-installer");
        t.setDaemon(true);
        t.start();
    }

    private void updateAll() {
        if (isBusy.get()) return;
        setBusy(true);
        addLog("🔄 Atualizando todos via winget...", "info");
        installer.updateAllAsync(msg -> Platform.runLater(() -> addLog(msg, "info")))
            .thenAccept(ok -> Platform.runLater(() -> {
                addLog(ok ? "✅ Atualização concluída!" : "⚠️ Atualização concluída com avisos.", ok ? "success" : "warning");
                setBusy(false);
            }));
    }

    private void downloadOffline(AppItem app) {
        javafx.stage.DirectoryChooser dc = new javafx.stage.DirectoryChooser();
        dc.setTitle("Selecione a pasta para salvar o instalador de " + app.getNome());
        java.io.File dir = dc.showDialog(flowApps.getScene().getWindow());
        if (dir == null) return;

        addLog("💾 Baixando offline: " + app.getNome(), "info");
        installer.downloadOfflineAsync(app, dir.toPath(), msg -> Platform.runLater(() -> addLog(msg, "info")))
            .thenAccept(ok -> Platform.runLater(() -> {
                addLog(ok ? "✅ Download offline concluído!" : "❌ Falha no download offline.", ok ? "success" : "error");
                if (ok) dataService.saveApps(allApps);
            }));
    }

    private void checkInstalled() {
        if (isBusy.get()) return;
        setBusy(true);
        addLog("🔍 Verificando apps instalados...", "info");

        Task<Void> task = new Task<>() {
            @Override
            protected Void call() {
                String installedAppsText = psService.executeAndCapture("winget list").toLowerCase();

                for (AppItem app : allApps) {
                    boolean isInst = false;
                    if ("winget".equals(app.getTipo()) && app.getWingetId() != null && !app.getWingetId().isBlank()) {
                        isInst = installedAppsText.contains(app.getWingetId().toLowerCase());
                    } else if ("local".equals(app.getTipo())) {
                        isInst = app.getLocalPath() != null && !app.getLocalPath().isBlank()
                            && java.nio.file.Files.exists(java.nio.file.Paths.get(app.getLocalPath()));
                    }

                    final boolean finalIsInst = isInst;
                    Platform.runLater(() -> {
                        app.setInstalado(finalIsInst);
                        if (finalIsInst && "Pendente".equals(app.getStatus()))
                            app.setStatus("✅ Instalado");
                    });
                }
                return null;
            }
            @Override protected void succeeded() {
                setBusy(false);
                addLog("✅ Verificação rápida concluída!", "success");
            }
        };
        Thread t = new Thread(task, "ktp-check-installed");
        t.setDaemon(true);
        t.start();
    }

    private void removeApp(AppItem app) {
        Alert alert = new Alert(Alert.AlertType.CONFIRMATION,
            "Remover \"" + app.getNome() + "\" da lista?",
            ButtonType.YES, ButtonType.NO);
        alert.setTitle("Confirmar");
        if (alert.showAndWait().orElse(ButtonType.NO) == ButtonType.YES) {
            cardCache.remove(app);
            allApps.remove(app);
            applyFilter();
            dataService.saveApps(allApps);
            addLog("🗑️ " + app.getNome() + " removido.", "warning");
        }
    }

    private void cancelBusy() {
        isBusy.set(false);
        psService.cancelCurrentProcesses();
        setBusy(false);
        addLog("⏹️ Operação cancelada e processos encerrados.", "warning");
    }

    // ── Sistema (Check único em 1 processo PowerShell) ───────────────────────

    private void checkSystemAsync() {
        Thread t = new Thread(() -> {
            String script = """
                $adm = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
                $net = try { (Test-Connection -ComputerName 8.8.8.8 -Count 1 -Quiet -ErrorAction Stop) } catch { $false }
                $wg  = try { (winget --version) } catch { 'N/A' }
                Write-Output "$adm|$net|$wg"
                """;
            String out = psService.executeAndCapture(script);
            String[] parts = out.split("\\|", -1);
            boolean admin  = parts.length > 0 && "True".equalsIgnoreCase(parts[0].trim());
            boolean inet   = parts.length > 1 && "True".equalsIgnoreCase(parts[1].trim());
            String  wVer   = parts.length > 2 ? parts[2].trim() : "N/A";
            boolean winget = wVer.startsWith("v");

            Platform.runLater(() -> {
                lblAdmin.setText("Admin: " + (admin ? "✅" : "❌"));
                lblAdmin.setStyle("-fx-text-fill: " + (admin ? "#22c55e" : "#ef4444") + ";");

                lblInternet.setText("Internet: " + (inet ? "✅" : "❌"));
                lblInternet.setStyle("-fx-text-fill: " + (inet ? "#22c55e" : "#ef4444") + ";");

                lblWinget.setText("Winget: " + (winget ? wVer : "❌ Não instalado"));
                lblWinget.setStyle("-fx-text-fill: " + (winget ? "#22c55e" : "#ef4444") + ";");

                addLog("ℹ️ Admin: %s | Internet: %s | Winget: %s"
                    .formatted(admin ? "Sim" : "Não", inet ? "Sim" : "Não", winget ? wVer : "Não"), "info");
            });
        }, "ktp-system-check");
        t.setDaemon(true);
        t.start();
    }

    // ── UI Helpers ───────────────────────────────────────────────────────────

    private void setBusy(boolean busy) {
        isBusy.set(busy);
        Platform.runLater(() -> {
            btnInstalarTudo.setDisable(busy);
            btnInstalarSelecionados.setDisable(busy);
            btnAtualizarTudo.setDisable(busy);
            btnCancelar.setDisable(!busy);
            barProgresso.setVisible(busy);
            lblStatus.setText(busy ? "Processando..." : "Pronto");
        });
    }

    private void addLog(String msg, String type) {
        String time = LocalTime.now().format(TIME_FMT);
        String color = switch (type) {
            case "success" -> "#22c55e";
            case "error"   -> "#ef4444";
            case "warning" -> "#f59e0b";
            default        -> "#60a5fa";
        };

        HBox row = new HBox(8);
        row.setPadding(new Insets(2, 6, 2, 6));
        Label timeLabel = new Label("[" + time + "]");
        timeLabel.setStyle("-fx-text-fill: #4b5563; -fx-font-size: 10px; -fx-font-family: monospace;");
        Label msgLabel = new Label(msg);
        msgLabel.setStyle("-fx-text-fill: " + color + "; -fx-font-size: 11px; -fx-font-family: monospace;");
        msgLabel.setWrapText(true);
        HBox.setHgrow(msgLabel, Priority.ALWAYS);
        row.getChildren().addAll(timeLabel, msgLabel);

        panelLog.getChildren().add(row);
        Platform.runLater(() -> scrollLog.setVvalue(1.0));

        if (panelLog.getChildren().size() > 500)
            panelLog.getChildren().remove(0);
    }

    private void showNotification(String appName, boolean success) {
        String msg = success ? appName + " instalado com sucesso!" : "Falha ao instalar " + appName;
        Notifications notif = Notifications.create()
            .title("Kit Técnico Pro")
            .text(msg)
            .graphic(new FontIcon(success ? Material2OutlinedAL.CHECK_CIRCLE : Material2OutlinedAL.ERROR))
            .hideAfter(javafx.util.Duration.seconds(4))
            .position(javafx.geometry.Pos.BOTTOM_RIGHT);

        if (success) notif.showInformation(); else notif.showError();
    }

    private void applyTheme(Scene scene) {
        URL css = getClass().getResource("/com/kittecnico/styles/kit-theme.css");
        if (css != null) scene.getStylesheets().add(css.toExternalForm());
    }

    // ── Handlers das Ferramentas da Toolbar ──────────────────────────────────
    @FXML public void openIsoManager() {
        addLog("Abrindo Gerenciador de ISOs...", "info");
        Platform.runLater(() -> {
            try {
                javafx.fxml.FXMLLoader loader = new javafx.fxml.FXMLLoader(getClass().getResource("/com/kittecnico/views/iso-manager.fxml"));
                javafx.scene.Parent root = loader.load();
                Scene scene = new Scene(root);
                applyTheme(scene);
                Stage stage = new Stage();
                stage.setTitle("Gerenciador de ISOs");
                stage.setScene(scene);
                stage.show();
            } catch (Exception e) {
                addLog("Erro ao abrir Gerenciador: " + e.getMessage(), "error");
            }
        });
    }

    @FXML public void cleanSystem() {
        setBusy(true);
        addLog("Iniciando Limpeza de Sistema (cleanmgr)...", "warning");
        psService.executeAsync("cleanmgr /sagerun:1", msg -> Platform.runLater(() -> addLog("[PS] " + msg, "info"))).thenAccept(res -> {
            Platform.runLater(() -> {
                setBusy(false);
                addLog("Limpeza de sistema concluída.", "success");
            });
        });
    }

    @FXML public void backupDrivers() {
        setBusy(true);
        addLog("Fazendo Backup de Drivers (pode demorar)...", "warning");
        String path = "C:\\BackupDrivers_" + java.time.LocalDate.now();
        String safePath = PowerShellService.escapeSingleQuoted(path);
        psService.executeAsync("New-Item -ItemType Directory -Force -Path '" + safePath + "'; Export-WindowsDriver -Online -Destination '" + safePath + "'", msg -> Platform.runLater(() -> addLog("[PS] " + msg, "info"))).thenAccept(res -> {
            Platform.runLater(() -> {
                setBusy(false);
                addLog("Backup salvo em: " + path, "success");
            });
        });
    }

    @FXML public void optimizeSystem() {
        addLog("Abrindo Otimização de Sistema...", "info");
        Stage stage = new Stage();
        stage.setTitle("Otimização de Sistema");

        VBox root = new VBox(12);
        root.setPadding(new Insets(18));
        root.setStyle("-fx-background-color: #0d1117;");

        Label title = new Label("Otimização e Manutenção Rápida");
        title.setStyle("-fx-text-fill: #38bdf8; -fx-font-size: 16px; -fx-font-weight: bold;");

        CheckBox chkTemp  = new CheckBox("Limpar arquivos temporários (%TEMP% e C:\\Windows\\Temp)");
        CheckBox chkDns   = new CheckBox("Limpar cache DNS (ipconfig /flushdns)");
        CheckBox chkSfc   = new CheckBox("Verificar integridade do sistema (sfc /scannow)");
        CheckBox chkTrim  = new CheckBox("Otimizar unidades / TRIM (defrag C: /O)");
        CheckBox chkHib   = new CheckBox("Desativar Hibernação para liberar espaço (powercfg -h off)");
        chkTemp.setSelected(true);
        chkDns.setSelected(true);

        for (CheckBox cb : List.of(chkTemp, chkDns, chkSfc, chkTrim, chkHib)) {
            cb.setStyle("-fx-text-fill: #e6edf3; -fx-font-size: 12px;");
        }

        Button btnRun = new Button("Executar Selecionados");
        btnRun.getStyleClass().add("btn-primary-green");
        btnRun.setOnAction(e -> {
            List<String> cmds = new ArrayList<>();
            if (chkTemp.isSelected()) cmds.add("Remove-Item -Path \"$env:TEMP\\*\" -Recurse -Force -ErrorAction SilentlyContinue");
            if (chkDns.isSelected())  cmds.add("ipconfig /flushdns");
            if (chkSfc.isSelected())  cmds.add("sfc /scannow");
            if (chkTrim.isSelected()) cmds.add("defrag C: /O");
            if (chkHib.isSelected())  cmds.add("powercfg -h off");

            if (cmds.isEmpty()) {
                addLog("⚠️ Nenhuma opção de otimização selecionada.", "warning");
                return;
            }
            stage.close();
            setBusy(true);
            String fullCmd = String.join("; ", cmds);
            addLog("⚡ Executando " + cmds.size() + " otimizações...", "info");
            psService.executeAsync(fullCmd, msg -> Platform.runLater(() -> addLog("[Otimização] " + msg, "info")))
                .thenAccept(ok -> Platform.runLater(() -> {
                    setBusy(false);
                    addLog(ok ? "✅ Otimização concluída!" : "⚠️ Otimização concluída com avisos.", ok ? "success" : "warning");
                }));
        });

        root.getChildren().addAll(title, chkTemp, chkDns, chkSfc, chkTrim, chkHib, btnRun);
        Scene scene = new Scene(root, 460, 290);
        applyTheme(scene);
        stage.setScene(scene);
        stage.show();
    }

    @FXML public void openProcessManager() {
        addLog("Abrindo Gerenciador de Processos...", "info");
        try {
            javafx.fxml.FXMLLoader loader = new javafx.fxml.FXMLLoader(getClass().getResource("/com/kittecnico/views/process-manager-view.fxml"));
            javafx.scene.Parent root = loader.load();
            Scene scene = new Scene(root, 800, 600);
            applyTheme(scene);
            Stage stage = new Stage();
            stage.setTitle("Gerenciador de Processos");
            stage.setScene(scene);
            stage.show();
        } catch (Exception e) {
            addLog("Erro ao abrir Gerenciador de Processos: " + e.getMessage(), "error");
        }
    }

    @FXML public void openDeviceManager() {
        addLog("Abrindo Gerenciador de Dispositivos...", "info");
        psService.executeAsync("Start-Process devmgmt.msc", msg -> Platform.runLater(() -> addLog("[PS] " + msg, "info")));
    }

    @FXML public void openControlPanel() {
        addLog("Abrindo Painel de Controle...", "info");
        psService.executeAsync("Start-Process control", msg -> Platform.runLater(() -> addLog("[PS] " + msg, "info")));
    }

    @FXML public void openSystemInfo() {
        addLog("Abrindo Informações do Sistema (msinfo32)...", "info");
        psService.executeAsync("Start-Process msinfo32", msg -> Platform.runLater(() -> addLog("[PS] " + msg, "info")));
    }

    @FXML public void openNetworkScanner() {
        addLog("Abrindo Scanner de Rede...", "info");
        try {
            javafx.fxml.FXMLLoader loader = new javafx.fxml.FXMLLoader(getClass().getResource("/com/kittecnico/views/network-scanner-view.fxml"));
            javafx.scene.Parent root = loader.load();
            Scene scene = new Scene(root, 700, 500);
            applyTheme(scene);
            Stage stage = new Stage();
            stage.setTitle("Scanner de Rede Local");
            stage.setScene(scene);
            stage.show();
        } catch (Exception e) {
            addLog("Erro ao abrir Scanner de Rede: " + e.getMessage(), "error");
        }
    }

    @FXML public void openStartupManager() {
        addLog("Abrindo Gerenciador de Inicialização...", "info");
        psService.executeAsync("Start-Process taskmgr -ArgumentList '/0 /startup'", msg -> Platform.runLater(() -> addLog("[PS] " + msg, "info")));
    }

    @FXML public void minimizeApp() {
        Stage stage = (Stage) titleBar.getScene().getWindow();
        stage.setIconified(true);
    }

    @FXML public void maximizeApp() {
        Stage stage = (Stage) titleBar.getScene().getWindow();
        stage.setMaximized(!stage.isMaximized());
    }

    @FXML public void exitApp() {
        addLog("Encerrando...", "warning");
        psService.cancelCurrentProcesses();
        Platform.exit();
        System.exit(0);
    }
}
