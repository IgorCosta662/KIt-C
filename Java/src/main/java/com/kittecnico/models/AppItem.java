package com.kittecnico.models;

import com.fasterxml.jackson.annotation.JsonAlias;
import com.fasterxml.jackson.annotation.JsonIgnore;
import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import javafx.beans.property.*;

/**
 * Modelo de um Aplicativo gerenciado pelo Kit Técnico Pro.
 * Usa JavaFX Properties para binding reativo com a UI.
 */
@JsonIgnoreProperties(ignoreUnknown = true)
public class AppItem {

    // ── Propriedades JavaFX (reativas) ─────────────────────────────────────
    private final StringProperty  nome         = new SimpleStringProperty("");
    private final StringProperty  categoria    = new SimpleStringProperty("");
    private final StringProperty  tipo         = new SimpleStringProperty("winget"); // winget | custom | local
    private final StringProperty  wingetId     = new SimpleStringProperty("");
    private final StringProperty  url          = new SimpleStringProperty("");
    private final StringProperty  localPath    = new SimpleStringProperty("");
    private final StringProperty  descricao    = new SimpleStringProperty("");
    private final StringProperty  icone        = new SimpleStringProperty("📦");
    private final StringProperty  iconUrl      = new SimpleStringProperty("");
    private final StringProperty  status       = new SimpleStringProperty("Pendente");
    private final BooleanProperty selecionado  = new SimpleBooleanProperty(false);
    private final BooleanProperty instalado    = new SimpleBooleanProperty(false);
    private final BooleanProperty emProgresso  = new SimpleBooleanProperty(false);
    private final DoubleProperty  progresso    = new SimpleDoubleProperty(0.0);
    private final StringProperty  versao       = new SimpleStringProperty("");
    private final BooleanProperty silencioso   = new SimpleBooleanProperty(true);
    private final StringProperty  argExtra     = new SimpleStringProperty("");

    // ── Construtores ────────────────────────────────────────────────────────
    public AppItem() {}

    public AppItem(String nome, String categoria, String tipo, String wingetId) {
        setNome(nome);
        setCategoria(categoria);
        setTipo(tipo);
        setWingetId(wingetId);
    }

    // ── Propriedades JavaFX ─────────────────────────────────────────────────
    public StringProperty  nomeProperty()        { return nome; }
    public StringProperty  categoriaProperty()   { return categoria; }
    public StringProperty  tipoProperty()        { return tipo; }
    public StringProperty  wingetIdProperty()    { return wingetId; }
    public StringProperty  urlProperty()         { return url; }
    public StringProperty  localPathProperty()   { return localPath; }
    public StringProperty  descricaoProperty()   { return descricao; }
    public StringProperty  iconeProperty()       { return icone; }
    public StringProperty  iconUrlProperty()     { return iconUrl; }
    public StringProperty  statusProperty()      { return status; }
    public BooleanProperty selecionadoProperty() { return selecionado; }
    public BooleanProperty instaladoProperty()   { return instalado; }
    public BooleanProperty emProgressoProperty() { return emProgresso; }
    public DoubleProperty  progressoProperty()   { return progresso; }
    public StringProperty  versaoProperty()      { return versao; }
    public BooleanProperty silenciosoProperty()  { return silencioso; }
    public StringProperty  argExtraProperty()    { return argExtra; }

    // ── Getters / Setters ────────────────────────────────────────────────────
    public String  getNome()       { return nome.get(); }
    public void    setNome(String v)       { nome.set(v == null ? "" : v); }

    public String  getCategoria()  { return categoria.get(); }
    public void    setCategoria(String v)  { categoria.set(v == null ? "" : v); }

    public String  getTipo()       { return tipo.get(); }
    public void    setTipo(String v)       { tipo.set(v == null ? "winget" : v); }

    @JsonProperty("id")
    public String  getWingetId()   { return wingetId.get(); }

    @JsonAlias({"id", "wingetId"})
    public void    setWingetId(String v)   { wingetId.set(v == null ? "" : v); }

    public String  getUrl()        { return url.get(); }
    public void    setUrl(String v)        { url.set(v == null ? "" : v); }

    public String  getLocalPath()  { return localPath.get(); }
    public void    setLocalPath(String v)  { localPath.set(v == null ? "" : v); }

    public String  getDescricao()  { return descricao.get(); }
    public void    setDescricao(String v)  { descricao.set(v == null ? "" : v); }

    public String  getIcone()      { return icone.get(); }
    public void    setIcone(String v)      { icone.set(v == null ? "📦" : v); }

    @JsonIgnore
    public String getResolvedIconLiteral() {
        String raw = getIcone();
        if (raw != null && (raw.startsWith("mdal-") || raw.startsWith("mdmz-"))) {
            return raw;
        }
        String n = getNome() != null ? getNome().toLowerCase() : "";
        if (n.contains("chrome")) return "mdal-web";
        if (n.contains("firefox")) return "mdal-web";
        if (n.contains("edge") || n.contains("opera") || n.contains("brave")) return "mdal-web";
        if (n.contains("spotify")) return "mdal-headset";
        if (n.contains("discord") || n.contains("whatsapp") || n.contains("telegram") || n.contains("skype")) return "mdal-chat";
        if (n.contains("zoom") || n.contains("obs ")) return "mdal-videocam";
        if (n.contains("vlc")) return "mdal-play_circle_outline";
        if (n.contains("steam") || n.contains("epic") || n.contains("game")) return "mdal-videogame_asset";
        if (n.contains("github") || n.contains("git") || n.contains("vscode") || n.contains("node") || n.contains("java")) return "mdal-code";
        if (n.contains("winrar") || n.contains("7-zip")) return "mdal-archive";
        if (n.contains("office") || n.contains("word") || n.contains("excel")) return "mdal-description";

        String c = getCategoria() != null ? getCategoria().toLowerCase() : "";
        if (c.contains("naveg")) return "mdal-public";
        if (c.contains("segur") || c.contains("antiv") || c.contains("vpn")) return "mdal-security";
        if (c.contains("multim") || c.contains("audio") || c.contains("video")) return "mdal-movie";
        if (c.contains("imag") || c.contains("design")) return "mdal-image";
        if (c.contains("desenv") || c.contains("program")) return "mdal-code";
        if (c.contains("banco") || c.contains("dados")) return "mdal-storage";
        if (c.contains("game") || c.contains("jogo")) return "mdal-videogame_asset";
        if (c.contains("office") || c.contains("escrit")) return "mdal-folder";
        if (c.contains("compact") || c.contains("arquiv")) return "mdal-archive";
        if (c.contains("driver") || c.contains("hardware")) return "mdal-memory";
        if (c.contains("diagn") || c.contains("monitor")) return "mdal-build";
        if (c.contains("rede") || c.contains("internet")) return "mdal-wifi";

        return "mdal-apps";
    }

    public String  getIconUrl()    { return iconUrl.get(); }
    public void    setIconUrl(String v)    { iconUrl.set(v == null ? "" : v); }

    public String  getStatus()     { return status.get(); }
    public void    setStatus(String v)     { status.set(v == null ? "Pendente" : v); }

    public boolean isSelecionado() { return selecionado.get(); }
    public void    setSelecionado(boolean v) { selecionado.set(v); }

    public boolean isInstalado()   { return instalado.get(); }
    public void    setInstalado(boolean v)   { instalado.set(v); }

    public boolean isEmProgresso() { return emProgresso.get(); }
    public void    setEmProgresso(boolean v) { emProgresso.set(v); }

    public double  getProgresso()  { return progresso.get(); }
    public void    setProgresso(double v)    { progresso.set(v); }

    public String  getVersao()     { return versao.get(); }
    public void    setVersao(String v)       { versao.set(v == null ? "" : v); }

    public boolean isSilencioso()  { return silencioso.get(); }
    public void    setSilencioso(boolean v)  { silencioso.set(v); }

    @JsonProperty("silentArgs")
    public String  getArgExtra()   { return argExtra.get(); }

    @JsonAlias({"silentArgs", "argExtra"})
    public void    setArgExtra(String v)     { argExtra.set(v == null ? "" : v); }

    /** Cor CSS do status baseada no valor */
    @JsonIgnore
    public String getStatusCssClass() {
        return switch (getStatus()) {
            case "✅ Instalado"    -> "status-ok";
            case "❌ Falhou"       -> "status-error";
            case "⏳ Instalando..." -> "status-progress";
            case "⬇ Baixado"      -> "status-downloaded";
            default               -> "status-pending";
        };
    }

    @Override
    public String toString() {
        return "AppItem{nome='%s', tipo='%s', status='%s'}".formatted(getNome(), getTipo(), getStatus());
    }
}


