package com.kittecnico.services;

import com.kittecnico.models.AppItem;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.*;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.time.Duration;
import java.util.concurrent.CompletableFuture;
import java.util.function.Consumer;

/**
 * Serviço de instalação de aplicativos via winget, URL direta ou executável local.
 */
public class InstallationService {

    private static final Logger log = LoggerFactory.getLogger(InstallationService.class);

    private final PowerShellService ps;
    private final HttpClient httpClient;

    public InstallationService(PowerShellService ps) {
        this.ps = ps;
        this.httpClient = HttpClient.newBuilder()
            .connectTimeout(Duration.ofSeconds(30))
            .followRedirects(HttpClient.Redirect.ALWAYS)
            .build();
    }

    /**
     * Instala um aplicativo de forma assíncrona.
     * @param app         Aplicativo a instalar
     * @param logConsumer Callback de log para a UI
     * @return Future<Boolean> - true se sucesso
     */
    public CompletableFuture<Boolean> installAsync(AppItem app, Consumer<String> logConsumer) {
        return CompletableFuture.supplyAsync(() -> {
            try {
                return switch (app.getTipo().toLowerCase()) {
                    case "winget" -> installViaWinget(app, logConsumer);
                    case "custom" -> installViaUrl(app, logConsumer);
                    case "local"  -> installLocal(app, logConsumer);
                    default -> {
                        logConsumer.accept("⚠️ Tipo desconhecido: " + app.getTipo());
                        yield false;
                    }
                };
            } catch (Exception e) {
                logConsumer.accept("❌ Exceção ao instalar " + app.getNome() + ": " + e.getMessage());
                log.error("Installation error for {}", app.getNome(), e);
                return false;
            }
        });
    }

    private boolean installViaWinget(AppItem app, Consumer<String> logConsumer) throws IOException, InterruptedException {
        String id = app.getWingetId();
        if (id == null || id.isBlank() || !PowerShellService.isValidWingetId(id)) {
            logConsumer.accept("❌ winget ID inválido ou não definido para: " + app.getNome());
            return false;
        }

        String silent = app.isSilencioso() ? "--silent" : "";
        String extra  = app.getArgExtra() == null ? "" : app.getArgExtra();
        String args   = extra.isBlank() ? "" : "--override '" + PowerShellService.escapeSingleQuoted(extra) + "'";
        String cmd    = "winget install --id '%s' --accept-source-agreements --accept-package-agreements %s %s"
                            .formatted(PowerShellService.escapeSingleQuoted(id), silent, args).trim();

        logConsumer.accept("▶ winget install " + id);
        return ps.execute(cmd, logConsumer);
    }

    private static String sanitizeFileName(String url) {
        if (url == null || url.isBlank()) return "installer.exe";
        String raw = url.substring(url.lastIndexOf('/') + 1);
        int qIdx = raw.indexOf('?');
        if (qIdx >= 0) raw = raw.substring(0, qIdx);
        String sanitized = raw.replaceAll("[\\\\/:*?\"<>|]", "_").trim();
        if (sanitized.isEmpty() || ".".equals(sanitized) || "..".equals(sanitized)) {
            return "installer.exe";
        }
        return sanitized;
    }

    private boolean installViaUrl(AppItem app, Consumer<String> logConsumer) throws Exception {
        logConsumer.accept("⬇ Baixando: " + app.getUrl());

        Path tmpDir  = Paths.get(System.getProperty("java.io.tmpdir"), "KitTecnicoPro");
        Files.createDirectories(tmpDir);

        String fileName = sanitizeFileName(app.getUrl());
        Path   filePath = tmpDir.resolve(fileName);

        HttpRequest request = HttpRequest.newBuilder()
            .uri(URI.create(app.getUrl()))
            .timeout(Duration.ofMinutes(10))
            .GET()
            .build();

        HttpResponse<Path> response = httpClient.send(
            request, HttpResponse.BodyHandlers.ofFile(filePath)
        );

        if (response.statusCode() != 200) {
            logConsumer.accept("❌ Download falhou com código HTTP: " + response.statusCode());
            return false;
        }

        logConsumer.accept("✅ Download concluído. Instalando...");
        return runInstaller(filePath.toString(), app.getArgExtra(), logConsumer);
    }

    private boolean installLocal(AppItem app, Consumer<String> logConsumer) throws IOException, InterruptedException {
        if (app.getLocalPath() == null || app.getLocalPath().isBlank()) {
            logConsumer.accept("❌ Caminho local não definido para: " + app.getNome());
            return false;
        }
        logConsumer.accept("📂 Instalando local: " + app.getLocalPath());
        return runInstaller(app.getLocalPath(), app.getArgExtra(), logConsumer);
    }

    private boolean runInstaller(String path, String args, Consumer<String> logConsumer) throws IOException, InterruptedException {
        String silent = (args == null || args.isBlank()) ? "/S /silent /quiet" : args;
        String safePath = PowerShellService.escapeSingleQuoted(path);
        String safeArgs = PowerShellService.escapeSingleQuoted(silent);
        String cmd = "$p = Start-Process -FilePath '%s' -ArgumentList '%s' -Wait -PassThru; exit $p.ExitCode"
                         .formatted(safePath, safeArgs);
        return ps.execute(cmd, logConsumer);
    }

    /**
     * Atualiza todos os apps via winget update.
     */
    public CompletableFuture<Boolean> updateAllAsync(Consumer<String> logConsumer) {
        logConsumer.accept("🔄 Iniciando atualização global via winget...");
        return ps.executeAsync(
            "winget upgrade --all --accept-source-agreements --accept-package-agreements --silent",
            logConsumer
        );
    }

    /**
     * Faz o download offline de um instalador para uma pasta específica.
     */
    public CompletableFuture<Boolean> downloadOfflineAsync(AppItem app, Path destDir, Consumer<String> logConsumer) {
        return CompletableFuture.supplyAsync(() -> {
            try {
                Files.createDirectories(destDir);
                logConsumer.accept("💾 Iniciando download offline de: " + app.getNome());

                if ("winget".equals(app.getTipo())) {
                    String id = app.getWingetId();
                    if (!PowerShellService.isValidWingetId(id)) {
                        logConsumer.accept("❌ winget ID inválido: " + id);
                        return false;
                    }
                    String cmd = "winget download --id '%s' -d '%s' --accept-source-agreements"
                                     .formatted(
                                         PowerShellService.escapeSingleQuoted(id),
                                         PowerShellService.escapeSingleQuoted(destDir.toString())
                                     );
                    return ps.execute(cmd, logConsumer);
                } else if (app.getUrl() != null && !app.getUrl().isBlank()) {
                    String fileName = sanitizeFileName(app.getUrl());
                    Path   filePath = destDir.resolve(fileName);

                    HttpRequest req = HttpRequest.newBuilder()
                        .uri(URI.create(app.getUrl()))
                        .timeout(Duration.ofMinutes(30))
                        .GET().build();

                    httpClient.send(req, HttpResponse.BodyHandlers.ofFile(filePath));
                    app.setLocalPath(filePath.toString());
                    app.setTipo("local");
                    logConsumer.accept("✅ Salvo em: " + filePath);
                    return true;
                }
                return false;
            } catch (Exception e) {
                logConsumer.accept("❌ Erro no download offline: " + e.getMessage());
                return false;
            }
        });
    }

    /**
     * Verifica se um app está instalado via winget.
     */
    public boolean isInstalled(AppItem app) {
        String id = app.getWingetId();
        if (!"winget".equals(app.getTipo()) || !PowerShellService.isValidWingetId(id)) return false;
        String result = ps.executeAndCapture("winget list --id '" + PowerShellService.escapeSingleQuoted(id) + "' --exact");
        return result.contains(id);
    }
}
