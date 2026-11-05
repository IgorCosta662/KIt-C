package com.kittecnico.controllers;

import javafx.application.Platform;
import javafx.beans.property.SimpleStringProperty;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.collections.transformation.FilteredList;
import javafx.fxml.FXML;
import javafx.scene.control.*;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.nio.charset.Charset;
import java.util.ArrayList;
import java.util.List;

public class ProcessManagerController {
    @FXML private TableView<ProcessItem> tableProcessos;
    @FXML private TextField txtBusca;
    @FXML private Label lblStatus;

    private final ObservableList<ProcessItem> processos = FXCollections.observableArrayList();
    private FilteredList<ProcessItem> filteredData;

    @FXML
    public void initialize() {
        filteredData = new FilteredList<>(processos, p -> true);
        tableProcessos.setItems(filteredData);

        txtBusca.textProperty().addListener((obs, old, newVal) -> {
            filteredData.setPredicate(p -> {
                if (newVal == null || newVal.isEmpty()) return true;
                String lower = newVal.toLowerCase();
                return p.getName().toLowerCase().contains(lower) || p.getPid().contains(lower);
            });
            lblStatus.setText("Exibindo " + filteredData.size() + " processos.");
        });

        loadProcesses();
    }

    @FXML
    public void loadProcesses() {
        processos.clear();
        lblStatus.setText("Carregando processos...");
        Thread t = new Thread(() -> {
            try {
                Process p = new ProcessBuilder("tasklist", "/FO", "CSV", "/NH")
                    .redirectErrorStream(true)
                    .start();
                List<ProcessItem> tempList = new ArrayList<>();
                try (BufferedReader reader = new BufferedReader(
                        new InputStreamReader(p.getInputStream(), Charset.forName("CP850")))) {
                    String line;
                    while ((line = reader.readLine()) != null) {
                        if (line.trim().isEmpty()) continue;
                        String[] parts = line.split("\",\"");
                        if (parts.length >= 5) {
                            String name = parts[0].replace("\"", "");
                            String pid = parts[1].replace("\"", "");
                            String session = parts[2].replace("\"", "");
                            String mem = parts[4].replace("\"", "");
                            tempList.add(new ProcessItem(name, pid, session, mem));
                        }
                    }
                }
                p.waitFor();
                Platform.runLater(() -> {
                    processos.setAll(tempList);
                    lblStatus.setText("Total de processos: " + processos.size());
                });
            } catch (Exception e) {
                Platform.runLater(() -> lblStatus.setText("Erro ao carregar processos: " + e.getMessage()));
            }
        });
        t.setDaemon(true);
        t.start();
    }

    @FXML
    public void killProcess() {
        ProcessItem selected = tableProcessos.getSelectionModel().getSelectedItem();
        if (selected == null) {
            Alert alert = new Alert(Alert.AlertType.WARNING, "Selecione um processo para encerrar.", ButtonType.OK);
            alert.show();
            return;
        }

        String pid = selected.getPid();
        if (pid == null || !pid.matches("\\d+")) {
            lblStatus.setText("PID inválido: " + pid);
            return;
        }

        Alert confirm = new Alert(Alert.AlertType.CONFIRMATION,
            "Tem certeza que deseja forçar o encerramento de " + selected.getName() + " (PID: " + pid + ")?",
            ButtonType.YES, ButtonType.NO);
        confirm.showAndWait().ifPresent(response -> {
            if (response == ButtonType.YES) {
                Thread t = new Thread(() -> {
                    try {
                        Process p = new ProcessBuilder("taskkill", "/F", "/PID", pid)
                            .redirectErrorStream(true)
                            .start();
                        int code = p.waitFor();
                        Platform.runLater(() -> {
                            lblStatus.setText(code == 0
                                ? "Processo " + selected.getName() + " encerrado."
                                : "Não foi possível encerrar (execute como Admin).");
                            loadProcesses();
                        });
                    } catch (Exception e) {
                        Platform.runLater(() -> lblStatus.setText("Erro ao encerrar processo: " + e.getMessage()));
                    }
                });
                t.setDaemon(true);
                t.start();
            }
        });
    }

    public static class ProcessItem {
        private final SimpleStringProperty name;
        private final SimpleStringProperty pid;
        private final SimpleStringProperty sessionName;
        private final SimpleStringProperty memory;

        public ProcessItem(String name, String pid, String sessionName, String memory) {
            this.name = new SimpleStringProperty(name);
            this.pid = new SimpleStringProperty(pid);
            this.sessionName = new SimpleStringProperty(sessionName);
            this.memory = new SimpleStringProperty(memory);
        }

        public String getName() { return name.get(); }
        public String getPid() { return pid.get(); }
        public String getSessionName() { return sessionName.get(); }
        public String getMemory() { return memory.get(); }
    }
}
