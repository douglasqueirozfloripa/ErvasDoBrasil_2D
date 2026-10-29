# Segurança

## Resumo
O jogo é **offline**: não tem servidor, API, login nem banco remoto, e não faz requisições. Por isso não existe API para sofrer DDoS nem requisição por onde dados possam vazar. A melhor proteção é manter assim, e é isso que as medidas abaixo garantem: se alguém tentar colocar rede no jogo, o lint, o build e os testes reprovam.

A auditoria de 29/10/2026 encontrou um problema real: **todo build saía como build de desenvolvimento**. Nesse modo a Unity liga a conexão do Profiler: o jogo escuta na **porta TCP 55000** e anuncia na rede local, por multicast, o IP e o nome do computador. Era a única porta de entrada de rede do jogo. Este era o `Player.log` antes da correção:
```
Player connection [...] "[IP] 192.168.x.x [Port] 55000 [...] [Id] OSXPlayer(1,<nome-do-mac>.local) [ProjectName] Ervas do Brasil"
```

## Superfície de ataque
| Onde | Risco | Antes | Agora |
|---|---|---|---|
| Rede: porta do Profiler | qualquer um no mesmo Wi-Fi descobre o jogo, conecta o Profiler e lê memória e desempenho; a porta aberta também recebe tráfego indesejado | aberta em todo build (`BuildOptions.Development`) | **fechada**: build release por padrão; desenvolvimento só com `--dev` |
| Rede: requisições | o código mandar dados para fora (HTTP, sockets, links) | nenhuma no código, mas nada impedia que alguém colocasse | **proibidas no lint** (regra RS0030) e nos pacotes (portão do build) |
| Rede: serviços da Unity | Analytics, Ads, IAP e relatório de crash enviam dados para a Unity | desligados | desligados, e o **portão do build reprova** se alguém ligar |
| Rede: estatísticas de hardware | a Unity coleta dados do aparelho na primeira abertura | ligado (`submitAnalytics: 1`) | desligado pelo `BuildTools.Configure` |
| Rede: HTTP sem criptografia | tráfego legível por quem está na rede | não permitido | não permitido, conferido no portão |
| Arquivos de save | save baixado da internet ou editado: números gigantes, `NaN`, listas enormes ou arquivo de vários MB travavam ou quebravam o jogo | só parte dos campos era limitada | **todos os campos validados**, arquivo de até 1 MB, id sem `/` nem `..` |
| Tela e log | caminho com o nome da conta do computador (`/Users/<nome>/...`) aparece em captura de tela ou num log enviado em relato de bug | caminho completo | a pasta pessoal aparece como `~` |
| Desserialização | `BinaryFormatter` pode executar código vindo de um arquivo adulterado | não usado | **proibido no lint**; os saves são JSON (`JsonUtility`) |

Editar o próprio save continua possível, de propósito: o jogo é single-player, e trapacear só afeta a própria partida. A mudança é que um save adulterado não trava nem quebra mais o jogo.

## As camadas
```
código ──▶ lint (RS0030) ──▶ portão antes do build ──▶ build release ──▶ conferência do app gerado ──▶ check-security.sh
```

### 1. Código: APIs proibidas no lint
`tools/lint/BannedSymbols.txt` lista as APIs que o código do jogo (`Core` e `Game`) não pode usar. O [Microsoft.CodeAnalysis.BannedApiAnalyzers](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md) reprova o `./scripts/lint.sh` com a regra **RS0030**:

| Proibido | Por quê |
|---|---|
| `System.Net`, `System.Net.Http`, `.Sockets`, `.WebSockets`, `.Security`, `.NetworkInformation`, `.Mail` | rede do .NET: `HttpClient`, `WebClient`, `Dns`, `TcpClient`... |
| `UnityEngine.Networking`, `WWW`, `Ping` | rede da Unity (`UnityWebRequest`) |
| `Application.OpenURL` | abrir um link manda a jogadora para fora do jogo |
| `System.Diagnostics.Process` | rodar `curl` ou `sh` seria rede por fora do C# |
| `BinaryFormatter` | desserialização insegura |

Exemplo de saída do lint:
```
error RS0030: The symbol 'System.Net.Http' is banned in this project: o jogo é offline: nada de requisições HTTP (docs/15-seguranca.md)
```
O código do Editor e dos testes fica de fora: ele não vai para dentro do jogo.

### 2. Projeto: portão antes de todo build
`Assets/Scripts/Editor/SecurityGate.cs` é um `IPreprocessBuildWithReport`, então roda em **todo** build: pelos scripts, pelo menu ou pelo Build Settings. O build é reprovado se:
- `Packages/manifest.json` ou `Packages/packages-lock.json` (que lista também as dependências) tiver pacote de rede ou telemetria (`com.unity.modules.unitywebrequest*`, `com.unity.services.*`, `com.unity.analytics`, Ads, IAP, Netcode, Transport, Firebase...). A lista fica em `Core/SecurityRules.cs`;
- o Player Settings permitir HTTP sem criptografia;
- Unity Analytics, Performance Reporting, Cloud Diagnostics, Ads ou IAP estiverem ligados;
- for um build de desenvolvimento que não foi pedido com `--dev`. Os players dos testes de interface (`run-ui-tests.sh`) são a exceção: eles precisam da conexão para devolver os resultados.

### 3. Build release por padrão
```bash
./scripts/build-macos.sh          # release: sem Profiler, sem porta aberta
./scripts/build-macos.sh --dev    # desenvolvimento: Profiler e console de erros na tela, com a porta 55000 aberta
./scripts/build-ios-sim.sh --dev  # idem para o Simulador
```
Use `--dev` só numa rede de confiança e não distribua esse build.

### 4. Conferência do app gerado
Depois de um build release, o `BuildTools` lê o `boot.config` do app. Se houver qualquer linha `player-connection-*` (a configuração que liga a porta do Profiler), o build é reprovado.

### 5. Saves vindos de fora
`SaveStore` (leitura do arquivo) e `SaveGame.Restore` (conversão para a partida) tratam o arquivo como entrada não confiável:

| Limite | Valor | Evita |
|---|---|---|
| tamanho do arquivo | 1 MB (um save real tem poucos KB) | arquivo gigante travar o menu de jogos salvos |
| id do save (nome do arquivo) | sem `/`, `\`, `:`, `..`, até 100 caracteres | gravar ou apagar arquivo fora da pasta dos saves |
| moedas, reputação, xp, dia, dias de amuleto e de sorte, recompensas | 0 a 1.000.000.000 | `int.MaxValue` estourar na primeira soma e virar negativo |
| quantidade de cada item | até 1.000.000 | o mesmo estouro na bolsa |
| itens de cada lista (bolsa, canteiros, pedidos, varal, herbário, males) | até 500 | milhares de objetos criados na cena |
| números com vírgula (energia, hora, crescimento...) | `NaN` e infinito viram 0; bônus de velocidade de 0 a +200%; posição de canteiro até ±1000 | posições e barras inválidas |
| nome do aldeão num pedido | até 40 caracteres | texto enorme travar a janela |
| histórico de avisos (botão Histórico) | os 60 últimos; texto até 300 caracteres, hora até 5; contagem de repetidos de 1 a 1.000.000.000; vazios e nulos ignorados | lista ou texto enormes travarem a janela do Histórico |

Os testes `SecurityTests.cs` cobrem cada limite; os do histórico ficam em `MessageLogTests.cs`.

### 6. Privacidade na tela e no log
A aba **Ao vivo** dos Bastidores e os avisos do `SaveStore` no `Player.log` mostram a pasta pessoal como `~` (`Privacy.Redact`). O `Player.log` continua no computador da jogadora, e as linhas que a própria Unity escreve (como `Mono path`) mantêm o caminho completo.

## Como verificar
```bash
./scripts/lint.sh              # RS0030: nenhuma API de rede no código
./scripts/run-tests.sh         # SecurityTests: limites do save, pacotes e Player Settings
./scripts/build-macos.sh       # portão antes do build + conferência do boot.config depois
./scripts/check-security.sh    # com o app aberto: nenhuma porta, nenhuma conexão, nenhum anúncio na rede
```
O `check-security.sh` olha o `boot.config`, observa os sockets do processo com `lsof` por 10 segundos e procura o anúncio `Player connection` no `Player.log`. Se o jogo estiver fechado, ele abre o app, examina desde a abertura e o fecha. Se estiver aberto, examina o processo que está rodando e não o fecha. Saída esperada:
```
▶ 1/3 boot.config do app...
✅ Sem conexão do Profiler.
▶ 2/3 Abri o jogo (pid 12345); observando a rede por 10s desde a abertura...
✅ Nenhuma porta escutando e nenhuma conexão.
▶ 3/3 Player.log...
✅ Nenhum anúncio na rede.
✅ Segurança de rede OK
```
Para conferir à mão: `lsof -nP -a -p $(pgrep -f ErvasDoBrasil.app/Contents/MacOS) -i` não deve listar nada.

## E se um dia o jogo tiver servidor?
Ranking, save na nuvem ou eventos online trariam uma API, e com ela DDoS, vazamento de dados e trapaça passariam a ser riscos reais. Antes de liberar a rede (tirar a proibição do lint e do portão só para o módulo que precisa dela):

| Tema | O que fazer |
|---|---|
| **DDoS** | a defesa fica no servidor: CDN/WAF na frente (Cloudflare, Cloud Armor), limite de requisições por IP e por conta (rate limit), timeouts curtos, teto no autoscaling para o ataque não virar conta alta |
| **Cliente educado** | no jogo, poucas chamadas, retry com espera exponencial e jitter, sem repetição em loop quando o servidor cair (senão os próprios jogadores viram um DDoS) |
| **Segredos** | nenhuma chave de API ou senha dentro do app: tudo que vai no binário pode ser extraído. A autenticação é por conta da jogadora, com tokens de curta duração |
| **Transporte** | só HTTPS (o `insecureHttpOption` já está em `NotAllowed` e o iOS exige ATS) |
| **Confiança** | o servidor valida tudo (moedas, xp, ranking): o cliente pode ser modificado |
| **Dados pessoais (LGPD)** | coletar o mínimo, dizer o que coleta, permitir apagar a conta |
| **Testes** | testes de API (contrato, autenticação, limites) e de carga (k6, JMeter) antes de abrir para o público |
