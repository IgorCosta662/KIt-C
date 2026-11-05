package com.kittecnico.controllers;

import javafx.application.Platform;
import javafx.beans.property.SimpleStringProperty;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.fxml.FXML;
import javafx.scene.control.*;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.nio.charset.Charset;
import java.util.ArrayList;
import java.util.List;

public class NetworkScannerController {
    @FXML private TableView<NetworkItem> tableNetwork;
    @FXML private Label lblStatus;

    private final ObservableList<NetworkItem> devices = FXCollections.observableArrayList();

    @FXML
    public void initialize() {
        tableNetwork.setItems(devices);
        scanNetwork();
    }

    @FXML
    public void scanNetwork() {
        devices.clear();
        lblStatus.setText("Escaneando rede (ARP)... Isso pode levar alguns segundos.");
        Thread t = new Thread(() -> {
            try {
                Process p = new ProcessBuilder("arp", "-a")
                    .redirectErrorStream(true)
                    .start();
                List<NetworkItem> tempList = new ArrayList<>();
                try (BufferedReader reader = new BufferedReader(
                        new InputStreamReader(p.getInputStream(), Charset.forName("CP850")))) {
                    String line;
                    while ((line = reader.readLine()) != null) {
                        if (line.trim().isEmpty() || line.toLowerCase().contains("interface")) continue;

                        String[] parts = line.trim().split("\\s+");
                        if (parts.length >= 3) {
                            String ip = parts[0];
                            String mac = parts[1];
                            String type = parts[2];
                            if (ip.matches("\\d+\\.\\d+\\.\\d+\\.\\d+")) {
                                tempList.add(new NetworkItem(ip, mac, type));
                            }
                        }
                    }
                }
                p.waitFor();
                Platform.runLater(() -> {
                    devices.setAll(tempList);
                    lblStatus.setText("Escaneamento concluído. Encontrados: " + devices.size() + " dispositivos.");
                });
            } catch (Exception e) {
                Platform.runLater(() -> lblStatus.setText("Erro ao escanear: " + e.getMessage()));
            }
        });
        t.setDaemon(true);
        t.start();
    }

    public static class NetworkItem {
        private final SimpleStringProperty ip;
        private final SimpleStringProperty mac;
        private final SimpleStringProperty type;

        public NetworkItem(String ip, String mac, String type) {
            this.ip = new SimpleStringProperty(ip);
            this.mac = new SimpleStringProperty(mac);
            this.type = new SimpleStringProperty(type);
        }

        public String getIp() { return ip.get(); }
        public String getMac() { return mac.get(); }
        public String getType() { return type.get(); }
    }
}
