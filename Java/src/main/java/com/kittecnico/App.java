package com.kittecnico;

import atlantafx.base.theme.PrimerDark;
import com.kittecnico.util.WindowStyleUtil;
import javafx.application.Application;
import javafx.application.Platform;
import javafx.fxml.FXMLLoader;
import javafx.scene.Scene;
import javafx.scene.image.Image;
import javafx.stage.Stage;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.util.Objects;

/**
 * Kit Técnico Pro — Versão Java
 * Framework: JavaFX 21 + AtlantaFX (Dark Mode)
 * Arquitetura: MVC + Services
 */
public class App extends Application {

    private static final Logger log = LoggerFactory.getLogger(App.class);

    public static final String VERSION = "2.0.0";
    public static final String APP_NAME = "Kit Técnico Pro";

    @Override
    public void start(Stage stage) throws IOException {
        // Aplica tema escuro AtlantaFX (Primer Dark — similar ao GitHub Dark)
        Application.setUserAgentStylesheet(new PrimerDark().getUserAgentStylesheet());

        FXMLLoader fxmlLoader = new FXMLLoader(
            App.class.getResource("views/main-view.fxml")
        );

        Scene scene = new Scene(fxmlLoader.load(), 950, 650);

        // CSS customizado
        scene.getStylesheets().add(
            Objects.requireNonNull(App.class.getResource("styles/kit-theme.css")).toExternalForm()
        );

        stage.setTitle(APP_NAME + " v" + VERSION);
        stage.setMinWidth(800);
        stage.setMinHeight(550);
        stage.setScene(scene);
        stage.initStyle(javafx.stage.StageStyle.UNDECORATED);

        // Ícone da janela
        try {
            stage.getIcons().add(
                new Image(Objects.requireNonNull(App.class.getResourceAsStream("images/icon.png")))
            );
        } catch (Exception e) {
            log.warn("Ícone não encontrado, usando padrão.");
        }

        // Centraliza na tela
        stage.centerOnScreen();

        stage.show();

        // Garante encerramento limpo
        stage.setOnCloseRequest(e -> {
            log.info("Encerrando Kit Técnico Pro...");
            Platform.exit();
            System.exit(0);
        });

        log.info("{} v{} iniciado!", APP_NAME, VERSION);
    }

    public static void main(String[] args) {
        launch(args);
    }
}

