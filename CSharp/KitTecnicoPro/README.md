# ⚡ Kit Técnico Pro

Ferramenta profissional para pós-formatação de computadores Windows.

## 🚀 Como abrir no Visual Studio

1. Abra o Visual Studio 2022 (ou superior)
2. Clique em **Abrir projeto ou solução**
3. Selecione o arquivo `KitTecnicoPro.csproj`
4. Aguarde o NuGet restaurar os pacotes automaticamente
5. Pressione **F5** para compilar e executar

### Requisitos
- Visual Studio 2022 com workload **.NET Desktop Development**
- .NET SDK 8.0+
- Windows 10/11

## 📁 Estrutura de pastas

```
KitTecnicoPro/
├── Models/          → Entidades de dados (AppItem, Profile, etc.)
├── Services/        → Lógica de negócio (instalação, sistema, etc.)
├── ViewModels/      → MVVM ViewModel + RelayCommand
├── Views/           → Janelas XAML + code-behind
├── Converters/      → Value converters para bindings WPF
├── Data/
│   ├── apps.json    → Lista de programas
│   └── profiles.json → Perfis de instalação
├── Plugins/         → Scripts .ps1 externos executáveis
├── offline/         → Instaladores locais (detectados automaticamente)
├── Logs/            → Logs diários gerados automaticamente
└── app.manifest     → Solicita execução como Administrador
```

## 🔧 Funcionalidades

| Funcionalidade | Descrição |
|---|---|
| Instalar Selecionados | Instala apenas os apps marcados |
| Instalar Tudo | Instala todos os apps da lista |
| Instalar por Perfil | Básico / Gamer / Programador / Escritório |
| Atualizar Programas | `winget upgrade --all` |
| Limpeza do Sistema | Remove temp, esvaziando lixeira |
| Verificar Instalados | Marca quais já estão instalados |
| Plugins | Executa scripts .ps1 da pasta /Plugins |
| Importar/Exportar | Compartilha lista de apps em JSON |
| Informações do Sistema | CPU, RAM, disco, SO |
| Ponto de Restauração | Criado automaticamente antes de instalar |

## 📦 Suporte Offline

Coloque instaladores na pasta `/offline` com o nome contendo o ID do programa.
Exemplo: `google_chrome_setup.exe` → detectado para `Google.Chrome`

## 🔌 Plugins

Coloque arquivos `.ps1` na pasta `/Plugins`.
Acesse via botão **Plugins** no header.
Os scripts são validados antes de executar.

## 🔐 Segurança

- Executa como Administrador (app.manifest)
- Valida comandos PowerShell antes de executar
- Confirma ações destrutivas com dialog
- Log completo em `/Logs/ktp_YYYY-MM-DD.log`
