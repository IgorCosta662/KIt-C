package com.kittecnico.services;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.kittecnico.models.AppItem;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

/**
 * Serviço de persistência — Lê e salva apps.json.
 */
public class AppDataService {

    private static final Logger log = LoggerFactory.getLogger(AppDataService.class);
    private static final ObjectMapper mapper = new ObjectMapper()
        .enable(SerializationFeature.INDENT_OUTPUT);

    private final Path dataDir;
    private final Path appsFile;

    public AppDataService() {
        // Pasta data/ ao lado do executável (ou do diretório atual em desenvolvimento)
        dataDir  = com.kittecnico.util.AppPaths.dataDir();
        appsFile = dataDir.resolve("apps.json");
    }

    /** Carrega a lista de apps (ou retorna a lista padrão se o arquivo não existir) */
    public List<AppItem> loadApps() {
        if (Files.exists(appsFile)) {
            try {
                AppItem[] items = mapper.readValue(appsFile.toFile(), AppItem[].class);
                log.info("Carregados {} apps de {}", items.length, appsFile);
                return new ArrayList<>(Arrays.asList(items));
            } catch (IOException e) {
                log.error("Erro ao ler apps.json", e);
            }
        }
        log.info("apps.json não encontrado — carregando lista padrão");
        return buildDefaultApps();
    }

    /** Salva a lista de apps em apps.json */
    public void saveApps(List<AppItem> apps) {
        try {
            Files.createDirectories(dataDir);
            mapper.writeValue(appsFile.toFile(), apps);
            log.debug("Salvos {} apps em {}", apps.size(), appsFile);
        } catch (IOException e) {
            log.error("Erro ao salvar apps.json", e);
        }
    }

    /** Lista padrão pré-configurada (igual ao projeto C#) */
    private List<AppItem> buildDefaultApps() {
        List<AppItem> list = new ArrayList<>();

        // ── Navegadores ──────────────────────────────────────────────────────
        list.add(app("Google Chrome",   "Navegadores", "winget", "Google.Chrome",      "🌐", "Navegador mais popular do mundo"));
        list.add(app("Mozilla Firefox", "Navegadores", "winget", "Mozilla.Firefox",    "🦊", "Navegador open-source rápido e privado"));
        list.add(app("Brave",           "Navegadores", "winget", "Brave.Brave",        "🦁", "Navegador com bloqueio de anúncios nativo"));
        list.add(app("Microsoft Edge",  "Navegadores", "winget", "Microsoft.Edge",     "🌊", "Navegador padrão do Windows 11"));

        // ── Compactadores ────────────────────────────────────────────────────
        list.add(app("WinRAR",  "Compactadores", "winget", "RARLab.WinRAR",  "📦", "Compactador mais usado do Windows"));
        list.add(app("7-Zip",   "Compactadores", "winget", "7zip.7zip",      "🗜️", "Compactador gratuito e open-source"));
        list.add(app("NanaZip", "Compactadores", "winget", "M2Team.NanaZip", "📂", "Moderno, suporte ao Windows 11"));

        // ── Escritório ───────────────────────────────────────────────────────
        list.add(app("LibreOffice",   "Escritório", "winget", "TheDocumentFoundation.LibreOffice", "📝", "Suite office open-source completa"));
        list.add(app("WPS Office",    "Escritório", "winget", "Kingsoft.WPSOffice",                "📊", "Suite office compatível com Microsoft Office"));
        list.add(app("Adobe Reader",  "Escritório", "winget", "Adobe.Acrobat.Reader.64-bit",       "📄", "Leitor de PDF oficial da Adobe"));
        list.add(app("Foxit Reader",  "Escritório", "winget", "Foxit.FoxitReader",                 "📋", "Leitor PDF leve e rápido"));

        // ── Comunicação ──────────────────────────────────────────────────────
        list.add(app("WhatsApp",  "Comunicação", "winget", "9NKSQGP7F2NH",          "💬", "WhatsApp Desktop oficial"));
        list.add(app("Discord",   "Comunicação", "winget", "Discord.Discord",       "🎮", "Comunicação para gamers e comunidades"));
        list.add(app("Telegram",  "Comunicação", "winget", "Telegram.TelegramDesktop", "✈️", "Mensageiro seguro e rápido"));
        list.add(app("Zoom",      "Comunicação", "winget", "Zoom.Zoom",             "📹", "Videochamadas profissionais"));
        list.add(app("Skype",     "Comunicação", "winget", "Microsoft.Skype",       "📞", "Videochamadas Microsoft"));

        // ── Desenvolvimento ──────────────────────────────────────────────────
        list.add(app("VS Code",       "Desenvolvimento", "winget", "Microsoft.VisualStudioCode", "💻", "Editor de código mais popular"));
        list.add(app("Git",           "Desenvolvimento", "winget", "Git.Git",                    "🐙", "Controle de versão"));
        list.add(app("JDK 21",        "Desenvolvimento", "winget", "Microsoft.OpenJDK.21",       "☕", "Java Development Kit LTS"));
        list.add(app("Python 3",      "Desenvolvimento", "winget", "Python.Python.3.12",          "🐍", "Linguagem de programação versátil"));
        list.add(app("Node.js",       "Desenvolvimento", "winget", "OpenJS.NodeJS",              "🟢", "Runtime JavaScript"));
        list.add(app("IntelliJ IDEA", "Desenvolvimento", "winget", "JetBrains.IntelliJIDEA.Community", "🧠", "IDE Java pela JetBrains"));

        // ── Multimídia ───────────────────────────────────────────────────────
        list.add(app("VLC",          "Multimídia", "winget", "VideoLAN.VLC",         "🎬", "Player multimídia universal"));
        list.add(app("Spotify",      "Multimídia", "winget", "Spotify.Spotify",      "🎵", "Streaming de música"));
        list.add(app("Audacity",     "Multimídia", "winget", "Audacity.Audacity",    "🎙️", "Editor de áudio gratuito"));
        list.add(app("GIMP",         "Multimídia", "winget", "GIMP.GIMP",           "🎨", "Editor de imagens open-source"));
        list.add(app("OBS Studio",   "Multimídia", "winget", "OBSProject.OBSStudio","📡", "Gravação e streaming profissional"));

        // ── Utilitários ──────────────────────────────────────────────────────
        list.add(app("CCleaner",       "Utilitários", "winget", "Piriform.CCleaner",    "🧹", "Limpeza e otimização do Windows"));
        list.add(app("CPU-Z",          "Utilitários", "winget", "CPUID.CPU-Z",          "🔬", "Informações detalhadas de hardware"));
        list.add(app("GPU-Z",          "Utilitários", "winget", "TechPowerUp.GPU-Z",    "🎯", "Informações da placa de vídeo"));
        list.add(app("CrystalDisk",    "Utilitários", "winget", "CrystalDewWorld.CrystalDiskInfo", "💿", "Monitor de saúde do HD/SSD"));
        list.add(app("HWiNFO",         "Utilitários", "winget", "REALiX.HWiNFO",        "📊", "Monitoramento completo do sistema"));
        list.add(app("TeamViewer",     "Utilitários", "winget", "TeamViewer.TeamViewer","🖥️", "Acesso remoto profissional"));
        list.add(app("AnyDesk",        "Utilitários", "winget", "AnyDesk.AnyDesk",      "📲", "Acesso remoto leve e rápido"));
        list.add(app("balenaEtcher",   "Utilitários", "winget", "Balena.Etcher",        "💾", "Cria pen drives bootáveis"));

        // ── Segurança ────────────────────────────────────────────────────────
        list.add(app("Malwarebytes",  "Segurança", "winget", "Malwarebytes.Malwarebytes", "🛡️", "Remoção de malware"));
        list.add(app("Bitwarden",     "Segurança", "winget", "Bitwarden.Bitwarden",       "🔐", "Gerenciador de senhas gratuito"));

        return list;
    }

    private AppItem app(String nome, String cat, String tipo, String id, String icone, String desc) {
        AppItem a = new AppItem();
        a.setNome(nome);
        a.setCategoria(cat);
        a.setTipo(tipo);
        a.setWingetId(id);
        a.setIcone(icone);
        a.setDescricao(desc);
        return a;
    }
}
