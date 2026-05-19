# ⚡ Kit Técnico Pro

Ferramenta para **pós-formatação** e **rotina de técnico de TI** no Windows.
O app oferece uma UI (WPF) para:
- instalar aplicativos (principalmente via **winget**)
- executar scripts PowerShell (scripts e plugins)
- fazer rotinas de limpeza/otimização
- gerar logs e relatórios do que foi executado

> Este repositório também contém uma versão antiga WinForms em `1.0/` (documentada no README daquela pasta). A solução `KitTecnicoPro.sln` compila a versão **WPF**.

---

## ✅ Requisitos

- Windows 10/11
- .NET SDK 8.0+
- `winget` disponível (Windows 11 normalmente já inclui)
- Visual Studio 2022 (opcional) com workload **.NET Desktop Development**

> Algumas ações exigem **Administrador**. O projeto inclui `app.manifest` para solicitar elevação via UAC.

---

## 🚀 Como executar (WPF)

### Opção A — Atalhos da raiz (recomendado)

- `Iniciar.bat`: build em *Release* e abre o `.exe`
- `Dev.bat`: build em *Debug* e abre o `.exe`

### Opção B — Visual Studio

1. Abra `KitTecnicoPro.sln`
2. Pressione **F5**

### Opção C — CLI

```bash
dotnet restore
dotnet build .\KitTecnicoPro.sln -c Release

dotnet run --project .\KitTecnicoPro\KitTecnicoPro.csproj
```

### Publicar

Exemplo para `win-x64` (ajuste conforme necessário):

```bash
dotnet publish .\KitTecnicoPro\KitTecnicoPro.csproj -c Release -r win-x64 --self-contained false
```

---

## 🧩 Principais funcionalidades (WPF)

- Instalar apps selecionados / instalar tudo
- Instalar por **perfil** (Básico, Gamer, Programador, Escritório)
- Atualizar programas: `winget upgrade --all`
- Verificar o que já está instalado via `winget list`
- Limpeza/otimização (via PowerShell)
- **Scripts**: catálogo/edit/drag&drop de `.ps1` (pasta `Scripts/`)
- **Plugins**: execução rápida de `.ps1` (pasta `KitTecnicoPro/Plugins/`)
- Logs persistentes em arquivo + log em tela
- Relatório pós-instalação (TXT/HTML)

---

## ⚙️ Configuração por JSON (sem recompilar)

A versão WPF carrega estes arquivos de `KitTecnicoPro/Data/` (copiados para a saída no build):

- `apps.json`: catálogo de aplicativos
- `profiles.json`: perfis (listas de IDs)
- `settings.json`: parâmetros de execução

### `apps.json`

Formato (exemplo):

```json
{ "nome": "Google Chrome", "id": "Google.Chrome", "categoria": "Navegadores", "tipo": "winget", "url": null, "silentArgs": null, "descricao": "Navegador web do Google" }
```

Campos mais usados:
- `tipo`:
  - `winget` (padrão): instala via `winget install --id ... --silent`
  - `custom`: baixa de uma `url` e executa com `silentArgs`
  - `local`: executa um instalador em `localPath` (quando definido)

### `profiles.json`

Cada perfil referencia uma lista de `apps` (IDs do `winget`):

```json
{ "nome": "Programador", "descricao": "...", "icone": "💻", "apps": ["Microsoft.VisualStudioCode", "Git.Git"] }
```

### `settings.json`

- `onlineAppsJsonUrl`: URL opcional para atualizar a lista de apps remotamente (com fallback para o arquivo local)
- `maxParallelInstalls`: número máximo de instalações simultâneas
- `installTimeoutMinutes`: timeout por instalação (quando aplicável)
- `createRestorePoint`: cria ponto de restauração antes de instalar (quando habilitado no app)

---

## 📜 Scripts (pasta `Scripts/`)

A UI de Scripts usa a pasta **na raiz do repositório**:

- `Scripts/` (com subpastas por categoria, por exemplo `Scripts/Manutenção`, `Scripts/Rede`, etc.)

Você pode:
- apontar para um `.ps1` existente
- arrastar e soltar `.ps1` na janela de Scripts
- marcar execução como admin (quando suportado)

---

## 🔌 Plugins (pasta `KitTecnicoPro/Plugins/`)

Coloque scripts `.ps1` em `KitTecnicoPro/Plugins/`. Eles aparecem na UI de Plugins e podem ser executados.

Observações:
- o app faz **validação básica** para bloquear comandos destrutivos óbvios
- scripts são executados com `-ExecutionPolicy Bypass` (use com responsabilidade)

---

## 📦 Suporte offline (instaladores locais)

O app procura uma pasta chamada `offline` **ao lado do executável** (diretório base do app). Se encontrar um instalador compatível, ele é usado antes do `winget`.

Dica prática durante desenvolvimento:
- após build, crie/coloque instaladores em `KitTecnicoPro/bin/<Debug|Release>/net8.0-windows/offline/`

O match é feito pelo nome do arquivo conter o `id` (ou variações com `_`).

---

## 🧾 Logs

Os logs persistentes são gravados em uma pasta `Logs/` ao lado do executável, no padrão:

- `Logs/ktp_YYYY-MM-DD.log`

---

## 📁 Estrutura do repositório (alto nível)

- `KitTecnicoPro/`: app principal (WPF)
  - `Data/`: `apps.json`, `profiles.json`, `settings.json`
  - `Services/`, `ViewModels/`, `Views/`: arquitetura MVVM
  - `Plugins/`: plugins `.ps1`
- `Scripts/`: scripts PowerShell organizados por categoria
- `Setups/`: instaladores organizados por categoria + ISOs
- `Ferramentas/`: ferramentas manuais (e subpastas)
- `1.0/`: versão antiga (WinForms)

---

## ⚠ Avisos importantes

- **winget**: se o ambiente estiver sem winget/Store, as instalações por winget vão falhar.
- **Permissões**: ações de sistema (limpeza profunda, restore point, etc.) podem exigir administrador.
- **Terceiros/licenças**: pastas como `Ferramentas/` e `Setups/` podem conter ferramentas/instaladores de terceiros; use e distribua apenas o que você tem direito/licença.
  - Existe referência a `Ferramentas/Cracks` no código de caminhos; este repositório não deve ser usado para promover uso indevido. Recomenda-se remover/ignorar esse conteúdo se não for aplicável.
