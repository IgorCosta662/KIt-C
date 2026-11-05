# ⚡ Kit Técnico Pro v2.0

Ferramenta profissional para **pós-formatação**, **manutenção**, **otimização** e **rotina de técnico de TI** no Windows.

Este repositório contém duas implementações completas e independentes:

| Pasta | Stack Tecnológica |
|-------|-------------------|
| [`CSharp/`](CSharp/README.md) | **C# 12 / WPF (.NET 8)** — MaterialDesign, Mica Glassmorphism, DI Container (`AppHost`), `System.Text.Json` Source Generators, `ResilientHttpService` com SHA-256 e logging assíncrono `.log` + `.jsonl` |
| [`Java/`](Java/README.md) | **Java 21 / JavaFX (Maven)** — AtlantaFX, Ikonli, cofre criptografado AES-GCM e execução assíncrona |

---

## ▶ Como iniciar

Dê dois cliques em **`Iniciar.bat`** na raiz do projeto e escolha:

```text
[1] Versao C#
[2] Versao Java
[3] Preparar / atualizar dependencias (precisa de internet)
[0] Sair
```

Atalhos diretos via terminal: `Iniciar.bat csharp`, `Iniciar.bat java` ou `Iniciar.bat preparar`.

---

## 🧩 Principais funcionalidades

- **Catálogo de Aplicativos**: Instalação silenciosa em lote ou paralela via `winget`, URL direta (`ResilientHttpService` com progresso em `MB/s` e validação `SHA-256`) ou instalador local/offline.
- **Perfis Inteligentes**: Seleção rápida por perfil (*Básico*, *Gamer*, *Programador*, *Escritório*) e criação de perfis customizados.
- **Gerenciador de ISOs**: Catálogo com +110 sistemas (Windows, Linux, BSD, Utilitários) com download resiliente via stream (`ArrayPool<byte>`) e verificação de hash.
- **Dashboard de Hardware**: Telemetria em tempo real (CPU, RAM, Discos, Rede, GPU e Sistema) via `PeriodicTimer`.
- **Cofre Técnico Seguro**: Armazenamento criptografado de credenciais (DPAPI no C# / AES-256-GCM PBKDF2 no Java).
- **Otimização, Scripts & Plugins**: Execução auditada e segura de rotinas PowerShell (`.ps1`), ponto de restauração e limpeza de sistema.
- **Logs Estruturados & Relatórios**: Gravação assíncrona em `Logs/` (`.log` legível e `.jsonl` estruturado) + exportação de relatórios pós-instalação (TXT/HTML).

---

## 📦 Dependências portáteis (`Dependencias/`)

Tudo o que as duas versões precisam pode ficar isolado dentro da pasta `Dependencias/`, sem exigir instalação global no Windows:

| Pasta | Conteúdo |
|-------|----------|
| `Dependencias/dotnet` | .NET 8 SDK + Desktop Runtime |
| `Dependencias/nuget`  | Cache offline de pacotes NuGet da versão C# |
| `Dependencias/jdk`    | JDK 21 (Eclipse Temurin) |
| `Dependencias/maven`  | Apache Maven 3.9.6 |
| `Dependencias/m2`     | Repositório local Maven da versão Java |

> As pastas `Dependencias/`, `Portatil/`, `offline/`, `cache/` e `isos/` estão no `.gitignore` para manter o repositório Git leve e rápido.

---

## ⚙️ Configuração por JSON (`CSharp/KitTecnicoPro/Data/`)

- `apps.json`: catálogo de aplicativos (`winget`, `custom` ou `local`).
- `profiles.json`: perfis pré-configurados e personalizados.
- `isos.json`: catálogo customizável de imagens ISO.
- `settings.json`: parâmetros tipados (`maxParallelInstalls`, `httpTimeoutMinutes`, `logRetentionDays`, `createRestorePoint`, `onlineAppsJsonUrl`).
