# Configuração do NuGet Ohubia com LicenseId

Este documento descreve como configurar um projeto .NET para utilizar o **NuGet Ohubia**, onde o **LicenseId é obrigatório no endpoint do feed**.

---

## 1. Obter o LicenseId

O LicenseId deve ser obtido no arquivo de licença (`*.lic`).

Exemplo de conteúdo do arquivo `.lic`:

```
LicenseId=XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
```

Este valor será utilizado diretamente no link do NuGet.

---

## 2. Criar o arquivo `nuget.config`

Adicionar o arquivo `nuget.config` na **raiz da solução**.

Substituir `SEU_LICENSE_ID_AQUI` pelo LicenseId obtido no arquivo `.lic`.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add
      key="Ohubia Nuget"
      value="https://pkgs.ohubia.dev/SEU_LICENSE_ID_AQUI/v3/index.json"
    />
  </packageSources>

  <packageSourceMapping>
    <packageSource key="Ohubia Nuget">
      <package pattern="Ohd.*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

---

## 3. Instalar o pacote NuGet

No Visual Studio:

- Botão direito no projeto
- **Manage NuGet Packages**
- Aba **Browse**
- Selecionar o feed **Ohubia Nuget**
- Instalar o pacote desejado (`Ohd.*`)

---

## 4. Adicionar o arquivo de licença ao projeto

- Copiar o arquivo `.lic` para o projeto
- Configurar o arquivo:
  - **Build Action**: Content
  - **Copy to Output Directory**: Copy if newer

---

## 5. Configurar a licença no `Program.cs`

```csharp
var licenseFileName = Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory,
    "dev.license.lic"
);

OhDaiLicense.SetLicense(
    File.ReadAllText(licenseFileName)
);
```

---

## Observações importantes

- O **LicenseId no link do NuGet** deve ser exatamente o mesmo do arquivo `.lic`
- Sem LicenseId válido, o pacote não será restaurado ou a licença não será validada
- Este modelo não utiliza token ou senha

---
