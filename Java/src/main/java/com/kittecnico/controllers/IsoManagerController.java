package com.kittecnico.controllers;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.kittecnico.services.PowerShellService;
import com.kittecnico.util.AppPaths;
import javafx.application.Platform;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.collections.transformation.FilteredList;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.geometry.Insets;
import javafx.scene.control.*;
import javafx.scene.control.cell.PropertyValueFactory;
import javafx.scene.layout.GridPane;
import javafx.scene.layout.HBox;
import org.kordamp.ikonli.javafx.FontIcon;
import org.kordamp.ikonli.material2.Material2OutlinedAL;
import org.kordamp.ikonli.material2.Material2OutlinedMZ;

import java.net.URL;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Arrays;
import java.util.ResourceBundle;
import java.util.Set;
import java.util.TreeSet;

public class IsoManagerController implements Initializable {
    @FXML private TextField txtSearch;
    @FXML private ComboBox<String> cmbCategory;
    @FXML private Label lblCount;
    @FXML private Label lblGlobalStatus;
    @FXML private ProgressBar progressBar;
    @FXML private TableView<IsoModel> tableIsos;
    @FXML private TableColumn<IsoModel, String> colName;
    @FXML private TableColumn<IsoModel, String> colCategory;
    @FXML private TableColumn<IsoModel, String> colArchitecture;
    @FXML private TableColumn<IsoModel, String> colSize;
    @FXML private TableColumn<IsoModel, String> colStatus;
    @FXML private TableColumn<IsoModel, Void> colActions;

    private final PowerShellService psService = new PowerShellService();
    private final ObjectMapper mapper = new ObjectMapper().enable(SerializationFeature.INDENT_OUTPUT);
    private final ObservableList<IsoModel> allIsos = FXCollections.observableArrayList();
    private FilteredList<IsoModel> filteredIsos;

    @Override
    public void initialize(URL url, ResourceBundle rb) {
        colName.setCellValueFactory(new PropertyValueFactory<>("name"));
        colCategory.setCellValueFactory(new PropertyValueFactory<>("category"));
        colArchitecture.setCellValueFactory(new PropertyValueFactory<>("architecture"));
        colSize.setCellValueFactory(new PropertyValueFactory<>("size"));
        colStatus.setCellValueFactory(new PropertyValueFactory<>("status"));

        filteredIsos = new FilteredList<>(allIsos, p -> true);
        tableIsos.setItems(filteredIsos);

        setupActionColumn();
        loadData();
        setupFilters();
    }

    private void setupFilters() {
        refreshCategories();
        if (txtSearch != null) {
            txtSearch.textProperty().addListener((obs, old, val) -> applyFilter());
        }
        if (cmbCategory != null) {
            cmbCategory.valueProperty().addListener((obs, old, val) -> applyFilter());
        }
    }

    private void refreshCategories() {
        if (cmbCategory == null) return;
        Set<String> cats = new TreeSet<>();
        for (IsoModel iso : allIsos) {
            if (iso.getCategory() != null && !iso.getCategory().isBlank()) {
                cats.add(iso.getCategory());
            }
        }
        ObservableList<String> items = FXCollections.observableArrayList("Todos");
        items.addAll(cats);
        String current = cmbCategory.getValue();
        cmbCategory.setItems(items);
        cmbCategory.setValue(current != null && items.contains(current) ? current : "Todos");
    }

    private void applyFilter() {
        String search = txtSearch != null && txtSearch.getText() != null ? txtSearch.getText().trim().toLowerCase() : "";
        String cat = cmbCategory != null && cmbCategory.getValue() != null ? cmbCategory.getValue() : "Todos";

        filteredIsos.setPredicate(iso -> {
            boolean catOk = "Todos".equals(cat) || (iso.getCategory() != null && iso.getCategory().equalsIgnoreCase(cat));
            boolean searchOk = search.isEmpty()
                || (iso.getName() != null && iso.getName().toLowerCase().contains(search))
                || (iso.getCategory() != null && iso.getCategory().toLowerCase().contains(search))
                || (iso.getArchitecture() != null && iso.getArchitecture().toLowerCase().contains(search));
            return catOk && searchOk;
        });
        lblCount.setText(filteredIsos.size() + " / " + allIsos.size() + " ISOs listadas");
    }

    private void setupActionColumn() {
        colActions.setCellFactory(param -> new TableCell<>() {
            private final Button btnDownload = new Button(" Baixar", new FontIcon(Material2OutlinedMZ.SAVE_ALT));
            private final Button btnRufus = new Button(" Rufus", new FontIcon(Material2OutlinedMZ.USB));
            private final Button btnDiskpart = new Button(" Diskpart", new FontIcon(Material2OutlinedAL.DEVELOPER_BOARD));
            private final HBox pane = new HBox(5, btnDownload, btnRufus, btnDiskpart);

            {
                btnDownload.setStyle("-fx-background-color: #15803d; -fx-text-fill: white; -fx-font-size: 11px;");
                btnRufus.setStyle("-fx-background-color: #1d4ed8; -fx-text-fill: white; -fx-font-size: 11px;");
                btnDiskpart.setStyle("-fx-background-color: #c2410c; -fx-text-fill: white; -fx-font-size: 11px;");

                btnDownload.setOnAction(e -> {
                    IsoModel iso = getTableView().getItems().get(getIndex());
                    if (iso.getUrl() != null && !iso.getUrl().isBlank()) {
                        lblGlobalStatus.setText("Abrindo link de download: " + iso.getName());
                        psService.executeAsync("Start-Process '" + PowerShellService.escapeSingleQuoted(iso.getUrl()) + "'", msg -> {});
                        iso.setStatus("Baixado");
                        saveData();
                        tableIsos.refresh();
                        return;
                    }
                    lblGlobalStatus.setText("Verificando imagem: " + iso.getName() + "...");
                    progressBar.setVisible(true);
                    Thread t = new Thread(() -> {
                        try { Thread.sleep(1200); } catch (Exception ignored) {}
                        Platform.runLater(() -> {
                            lblGlobalStatus.setText("Download concluído: " + iso.getName());
                            progressBar.setVisible(false);
                            iso.setStatus("Baixado");
                            saveData();
                            tableIsos.refresh();
                        });
                    });
                    t.setDaemon(true);
                    t.start();
                });
                btnRufus.setOnAction(e -> psService.executeAsync("Start-Process 'https://rufus.ie'", msg -> {}));
                btnDiskpart.setOnAction(e -> psService.executeAsync("Start-Process diskpart", msg -> {}));
            }

            @Override
            protected void updateItem(Void item, boolean empty) {
                super.updateItem(item, empty);
                setGraphic(empty ? null : pane);
            }
        });
    }

    private void loadData() {
        try {
            Path isoPath = AppPaths.dataDir().resolve("isos.json");
            if (Files.exists(isoPath)) {
                IsoModel[] isos = mapper.readValue(isoPath.toFile(), IsoModel[].class);
                allIsos.setAll(Arrays.asList(isos));
            } else {
                lblGlobalStatus.setText("Banco de ISOs não encontrado.");
            }
        } catch (Exception e) {
            lblGlobalStatus.setText("Erro ao carregar ISOs: " + e.getMessage());
        }
        lblCount.setText(allIsos.size() + " ISOs listadas");
    }

    private void saveData() {
        try {
            Path isoPath = AppPaths.dataDir().resolve("isos.json");
            Files.createDirectories(isoPath.getParent());
            mapper.writeValue(isoPath.toFile(), allIsos);
        } catch (Exception e) {
            lblGlobalStatus.setText("Erro ao salvar ISOs: " + e.getMessage());
        }
    }

    @FXML
    public void openIsoFolder() {
        try {
            Path isoDir = AppPaths.dataDir().resolve("isos");
            Files.createDirectories(isoDir);
            new ProcessBuilder("explorer.exe", isoDir.toString()).start();
            lblGlobalStatus.setText("Pasta aberta: " + isoDir);
        } catch (Exception e) {
            lblGlobalStatus.setText("Erro ao abrir pasta: " + e.getMessage());
        }
    }

    @FXML
    public void addCustomIso() {
        Dialog<IsoModel> dialog = new Dialog<>();
        dialog.setTitle("Adicionar ISO Personalizada");
        dialog.setHeaderText("Informe os dados da imagem ISO");

        ButtonType btnSalvar = new ButtonType("Salvar", ButtonBar.ButtonData.OK_DONE);
        dialog.getDialogPane().getButtonTypes().addAll(btnSalvar, ButtonType.CANCEL);

        GridPane grid = new GridPane();
        grid.setHgap(10);
        grid.setVgap(10);
        grid.setPadding(new Insets(16));

        TextField txtNome = new TextField();
        txtNome.setPromptText("Ex: Windows 11 24H2 PT-BR");
        TextField txtCat = new TextField("Windows 11");
        TextField txtArch = new TextField("x64");
        TextField txtSize = new TextField("5.5 GB");
        TextField txtUrl = new TextField();
        txtUrl.setPromptText("https://... (opcional)");

        grid.add(new Label("Nome:"), 0, 0);        grid.add(txtNome, 1, 0);
        grid.add(new Label("Categoria:"), 0, 1);   grid.add(txtCat, 1, 1);
        grid.add(new Label("Arquitetura:"), 0, 2); grid.add(txtArch, 1, 2);
        grid.add(new Label("Tamanho:"), 0, 3);     grid.add(txtSize, 1, 3);
        grid.add(new Label("URL:"), 0, 4);         grid.add(txtUrl, 1, 4);

        dialog.getDialogPane().setContent(grid);

        dialog.setResultConverter(btn -> {
            if (btn == btnSalvar && !txtNome.getText().isBlank()) {
                IsoModel m = new IsoModel(
                    txtNome.getText().trim(),
                    txtCat.getText().isBlank() ? "Outros" : txtCat.getText().trim(),
                    txtArch.getText().isBlank() ? "x64" : txtArch.getText().trim(),
                    txtSize.getText().isBlank() ? "N/A" : txtSize.getText().trim(),
                    "Não Baixado"
                );
                m.setUrl(txtUrl.getText().trim());
                return m;
            }
            return null;
        });

        dialog.showAndWait().ifPresent(iso -> {
            allIsos.add(iso);
            saveData();
            refreshCategories();
            applyFilter();
            lblGlobalStatus.setText("ISO adicionada: " + iso.getName());
        });
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class IsoModel {
        private String name;
        private String category;
        private String architecture;
        private String size;
        private String status;
        private String url;

        public IsoModel() {}
        public IsoModel(String n, String c, String a, String s, String st) {
            name = n; category = c; architecture = a; size = s; status = st;
        }
        public String getName() { return name; }
        public void setName(String n) { name = n; }
        public String getCategory() { return category; }
        public void setCategory(String c) { category = c; }
        public String getArchitecture() { return architecture; }
        public void setArchitecture(String a) { architecture = a; }
        public String getSize() { return size; }
        public void setSize(String s) { size = s; }
        public String getStatus() { return status; }
        public void setStatus(String st) { status = st; }
        public String getUrl() { return url; }
        public void setUrl(String u) { url = u; }
    }
}
