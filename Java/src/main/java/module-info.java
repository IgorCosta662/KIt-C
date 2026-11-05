module com.kittecnico {
    requires javafx.controls;
    requires javafx.fxml;
    requires javafx.web;
    requires javafx.media;
    requires atlantafx.base;
    requires org.controlsfx.controls;
    requires org.kordamp.ikonli.javafx;
    requires org.kordamp.ikonli.material2;
    requires org.kordamp.ikonli.fluentui;
    requires com.fasterxml.jackson.databind;
    requires com.fasterxml.jackson.datatype.jsr310;
    requires org.slf4j;
    requires java.desktop;
    requires java.net.http;
    requires com.sun.jna;
    requires com.sun.jna.platform;

    opens com.kittecnico to javafx.fxml;
    opens com.kittecnico.controllers to javafx.fxml, javafx.base, com.fasterxml.jackson.databind;
    opens com.kittecnico.models to javafx.base, com.fasterxml.jackson.databind;

    exports com.kittecnico;
    exports com.kittecnico.controllers;
    exports com.kittecnico.models;
    exports com.kittecnico.services;
    exports com.kittecnico.util;
}
