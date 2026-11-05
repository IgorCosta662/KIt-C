# Kit Técnico Pro — Versão Java ☕

> Reimplementação em **Java 21 + JavaFX 21** do Kit Técnico Pro para pós-formatação e manutenção de sistemas Windows.

---

## 🚀 Stack Tecnológica

| Tecnologia | Versão | Função |
|---|---|---|
| **Java** | 21 (LTS) | Linguagem principal |
| **JavaFX** | 21.0.2 | Framework de UI |
| **AtlantaFX** | 2.0.1 | Tema dark moderno (PrimerDark) |
| **ControlsFX** | 11.2.1 | Notificações nativas, extras de UI |
| **Ikonli** | 12.3.1 | Ícones Material Design + FluentUI |
| **Jackson** | 2.16 | JSON (apps.json) |
| **Maven** | 3.9+ | Build & dependências |
| **JNA** | via util | Windows DWM API (Mica Glass) |
| **SLF4J + Logback** | 1.4 | Logging profissional |

---

## 📁 Estrutura do Projeto

```
KitTecnicoPro-Java/
├── pom.xml                          ← Dependências Maven
├── run.bat                          ← Script de build e execução
└── src/main/
    ├── java/
    │   └── com/kittecnico/
    │       ├── App.java             ← Ponto de entrada
    │       ├── controllers/
    │       │   └── MainController.java  ← Lógica da janela principal
    │       ├── models/
    │       │   ├── AppItem.java     ← Modelo reativo (JavaFX Properties)
    │       │   └── IsoItem.java     ← Modelo de ISOs
    │       ├── services/
    │       │   ├── PowerShellService.java     ← Executa PS em background
    │       │   ├── InstallationService.java   ← winget / URL / local
    │       │   └── AppDataService.java        ← Persistência JSON
    │       └── util/
    │           └── WindowStyleUtil.java       ← Efeito Mica Windows 11
    └── resources/
        └── com/kittecnico/
            ├── views/
            │   └── main-view.fxml   ← Layout da janela principal
            └── styles/
                └── kit-theme.css    ← Tema dark customizado
```

---

## 🎨 Melhorias em Relação ao C\#

✅ **Reatividade Total** — JavaFX Properties + bindings automáticos  
✅ **Async Nativo** — `CompletableFuture` para todas as operações de rede/shell  
✅ **Notificações nativas** via ControlsFX  
✅ **Efeito Mica do Windows 11** via JNA (DWM API)  
✅ **Icons profissionais** Ikonli Material Design  
✅ **CSS tematizado** completo (scrollbars, badges, cards, hover)  
✅ **Log em tempo real** com cores por nível  
✅ **Download offline** de instaladores  
✅ **Lista padrão** com 35+ apps pré-configurados  
✅ **Module System** (JPMS) — projeto hermético e seguro  

---

## ⚡ Como Rodar

### Pré-requisitos
- **JDK 21+**: `winget install Microsoft.OpenJDK.21`  
- **Maven 3.9+**: `winget install Apache.Maven`  

### Executar
```bash
# No terminal, na pasta KitTecnicoPro-Java/
mvn javafx:run

# Ou use o script
run.bat
```

### Build (gera .jar)
```bash
mvn package
java -jar target/KitTecnicoPro-2.0.0.jar
```
