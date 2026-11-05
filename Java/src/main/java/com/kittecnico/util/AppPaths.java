package com.kittecnico.util;

import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Resolve a pasta base do aplicativo.
 * <ul>
 *   <li>Empacotado (jpackage): pasta onde está o KitTecnicoPro.exe
 *       (o launcher do jpackage define a propriedade {@code jpackage.app-path}).</li>
 *   <li>Desenvolvimento (mvn javafx:run): diretório de trabalho atual.</li>
 * </ul>
 */
public final class AppPaths {

    private static final Path HOME = resolveHome();

    private AppPaths() {}

    private static Path resolveHome() {
        String exe = System.getProperty("jpackage.app-path");
        if (exe != null && !exe.isBlank()) {
            Path parent = Paths.get(exe).toAbsolutePath().getParent();
            if (parent != null && Files.isDirectory(parent)) return parent;
        }
        return Paths.get(System.getProperty("user.dir")).toAbsolutePath();
    }

    /** Pasta base do aplicativo. */
    public static Path home() { return HOME; }

    /** Pasta de dados ({@code data/}) ao lado do executável. */
    public static Path dataDir() { return HOME.resolve("data"); }
}

