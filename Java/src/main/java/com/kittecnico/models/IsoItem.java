package com.kittecnico.models;

import javafx.beans.property.*;

/** Modelo de uma ISO de Sistema Operacional */
public class IsoItem {

    private final StringProperty  nome        = new SimpleStringProperty("");
    private final StringProperty  descricao   = new SimpleStringProperty("");
    private final StringProperty  versao      = new SimpleStringProperty("");
    private final StringProperty  categoria   = new SimpleStringProperty("Windows");  // Windows | Linux | BSD | Utilitários
    private final StringProperty  arquitetura = new SimpleStringProperty("x64");
    private final StringProperty  tamanho     = new SimpleStringProperty("");
    private final StringProperty  urlDownload = new SimpleStringProperty("");
    private final StringProperty  urlSite     = new SimpleStringProperty("");
    private final StringProperty  localPath   = new SimpleStringProperty("");
    private final StringProperty  statusText  = new SimpleStringProperty("Disponível");
    private final DoubleProperty  progresso   = new SimpleDoubleProperty(0.0);
    private final BooleanProperty baixando    = new SimpleBooleanProperty(false);
    private final BooleanProperty baixado     = new SimpleBooleanProperty(false);
    private final BooleanProperty linkDireto  = new SimpleBooleanProperty(true);

    public IsoItem() {}

    public IsoItem(String nome, String versao, String categoria, String urlDownload, String urlSite) {
        setNome(nome); setVersao(versao); setCategoria(categoria);
        setUrlDownload(urlDownload); setUrlSite(urlSite);
    }

    /** Retorna o emoji correspondente à categoria */
    public String getCategoriaIcon() {
        return switch (getCategoria()) {
            case "Windows"     -> "🪟";
            case "Linux"       -> "🐧";
            case "BSD"         -> "🐡";
            case "Utilitários" -> "🔧";
            default            -> "💿";
        };
    }

    // ── Properties ──────────────────────────────────────────────────────────
    public StringProperty  nomeProperty()        { return nome; }
    public StringProperty  descricaoProperty()   { return descricao; }
    public StringProperty  versaoProperty()      { return versao; }
    public StringProperty  categoriaProperty()   { return categoria; }
    public StringProperty  arquiteturaProperty() { return arquitetura; }
    public StringProperty  tamanhoProperty()     { return tamanho; }
    public StringProperty  urlDownloadProperty() { return urlDownload; }
    public StringProperty  urlSiteProperty()     { return urlSite; }
    public StringProperty  localPathProperty()   { return localPath; }
    public StringProperty  statusTextProperty()  { return statusText; }
    public DoubleProperty  progressoProperty()   { return progresso; }
    public BooleanProperty baixandoProperty()    { return baixando; }
    public BooleanProperty baixadoProperty()     { return baixado; }
    public BooleanProperty linkDiretoProperty()  { return linkDireto; }

    // ── Getters / Setters ────────────────────────────────────────────────────
    public String  getNome()         { return nome.get(); }
    public void    setNome(String v) { nome.set(v); }
    public String  getDescricao()    { return descricao.get(); }
    public void    setDescricao(String v) { descricao.set(v); }
    public String  getVersao()       { return versao.get(); }
    public void    setVersao(String v) { versao.set(v); }
    public String  getCategoria()    { return categoria.get(); }
    public void    setCategoria(String v) { categoria.set(v); }
    public String  getArquitetura()  { return arquitetura.get(); }
    public void    setArquitetura(String v) { arquitetura.set(v); }
    public String  getTamanho()      { return tamanho.get(); }
    public void    setTamanho(String v) { tamanho.set(v); }
    public String  getUrlDownload()  { return urlDownload.get(); }
    public void    setUrlDownload(String v) { urlDownload.set(v); }
    public String  getUrlSite()      { return urlSite.get(); }
    public void    setUrlSite(String v) { urlSite.set(v); }
    public String  getLocalPath()    { return localPath.get(); }
    public void    setLocalPath(String v) { localPath.set(v); }
    public String  getStatusText()   { return statusText.get(); }
    public void    setStatusText(String v) { statusText.set(v); }
    public double  getProgresso()    { return progresso.get(); }
    public void    setProgresso(double v) { progresso.set(v); }
    public boolean isBaixando()      { return baixando.get(); }
    public void    setBaixando(boolean v) { baixando.set(v); }
    public boolean isBaixado()       { return baixado.get(); }
    public void    setBaixado(boolean v) { baixado.set(v); }
    public boolean isLinkDireto()    { return linkDireto.get(); }
    public void    setLinkDireto(boolean v) { linkDireto.set(v); }
}
