package com.kittecnico.services;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import java.util.Set;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ConcurrentHashMap;
import java.util.function.Consumer;

/**
 * Serviço responsável por executar scripts PowerShell no Windows.
 * Roda os comandos de forma assíncrona sem bloquear a UI.
 */
public class PowerShellService {

    private static final Logger log = LoggerFactory.getLogger(PowerShellService.class);
    private final Set<Process> activeProcesses = ConcurrentHashMap.newKeySet();

    /** Escapa aspas simples para uso seguro em strings single-quoted do PowerShell. */
    public static String escapeSingleQuoted(String s) {
        return s == null ? "" : s.replace("'", "''");
    }

    /** Valida se o identificador winget contém apenas caracteres seguros. */
    public static boolean isValidWingetId(String id) {
        return id != null && id.matches("^[A-Za-z0-9._-]+$");
    }

    /** Encerra imediatamente todos os processos PowerShell em execução. */
    public void cancelCurrentProcesses() {
        for (Process p : activeProcesses) {
            try {
                p.destroyForcibly();
            } catch (Exception e) {
                log.warn("Erro ao encerrar processo PowerShell", e);
            }
        }
        activeProcesses.clear();
    }

    /**
     * Executa um comando PowerShell de forma assíncrona.
     * @param command     O script PS a executar
     * @param logConsumer Callback chamado para cada linha de output (para atualizar a UI)
     * @return Future<Boolean> - true se saiu com código 0
     */
    public CompletableFuture<Boolean> executeAsync(String command, Consumer<String> logConsumer) {
        return CompletableFuture.supplyAsync(() -> {
            try {
                return execute(command, logConsumer);
            } catch (Exception e) {
                logConsumer.accept("❌ Erro ao executar PowerShell: " + e.getMessage());
                log.error("PowerShell execution error", e);
                return false;
            }
        });
    }

    /**
     * Executa um comando PowerShell de forma síncrona.
     */
    public boolean execute(String command, Consumer<String> logConsumer) throws IOException, InterruptedException {
        List<String> cmd = new ArrayList<>();
        cmd.add("powershell.exe");
        cmd.add("-NonInteractive");
        cmd.add("-NoProfile");
        cmd.add("-ExecutionPolicy"); cmd.add("Bypass");
        cmd.add("-Command");
        cmd.add("[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; " + command);

        ProcessBuilder pb = new ProcessBuilder(cmd);
        pb.redirectErrorStream(true);
        pb.environment().put("POWERSHELL_TELEMETRY_OPTOUT", "1");

        Process process = pb.start();
        activeProcesses.add(process);
        try {
            // Lê output em tempo real
            try (BufferedReader reader = new BufferedReader(
                    new InputStreamReader(process.getInputStream(), StandardCharsets.UTF_8))) {
                String line;
                while ((line = reader.readLine()) != null) {
                    final String l = line.trim();
                    if (!l.isEmpty()) {
                        logConsumer.accept(l);
                    }
                }
            }

            int exitCode = process.waitFor();
            log.debug("PowerShell exit code: {}", exitCode);
            return exitCode == 0;
        } finally {
            activeProcesses.remove(process);
        }
    }

    /**
     * Executa um script PS e retorna o output completo como String.
     */
    public String executeAndCapture(String command) {
        StringBuilder sb = new StringBuilder();
        try {
            execute(command, line -> sb.append(line).append("\n"));
        } catch (Exception e) {
            log.error("Capture error", e);
        }
        return sb.toString().trim();
    }

    /** Verifica se o winget está instalado */
    public boolean isWingetAvailable() {
        String result = executeAndCapture("winget --version");
        return result.startsWith("v");
    }

    /** Retorna a versão do winget */
    public String getWingetVersion() {
        return executeAndCapture("winget --version");
    }

    /** Verifica se está rodando como Administrador */
    public boolean isAdministrator() {
        String result = executeAndCapture(
            "([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)"
        );
        return "True".equalsIgnoreCase(result);
    }

    /** Verifica conexão com a internet */
    public boolean hasInternetConnection() {
        String result = executeAndCapture("(Test-NetConnection -ComputerName 'google.com' -Port 80 -InformationLevel Quiet -WarningAction SilentlyContinue).TcpTestSucceeded");
        return "True".equalsIgnoreCase(result);
    }
}
