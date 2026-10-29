# Mecânicas e regras

> Valores exatos ficam em `Assets/Scripts/Core` e são cobertos por testes.

## Personagem: 3 corpos
- **Energia física:** começa cheia (100). Gasta em ações (preparar 4, plantar 2, regar 1, colher 2, coletar 3, pilar 3, caldeirão 5). Regenera 1,5/s. Quando cai de uma vez, sem ser por uma ação (crise de fissura, assombração, Cuca, Bernúncia), a barra **pisca em vermelho** e mostra **"−N"** no pedaço que esvaziou: a recarga que vem depois não parece um carregamento.
- **Sanidade/Calma:** cai 0,3/s à noite e perto de visagens. Ao **cruzar** 30 para baixo ganha **Ansiedade** (perde em dobro e a tela treme). Curada a ansiedade, ela só volta se a sanidade subir acima de 30 e cair de novo. Ansiosa, a sanidade recupera de dia pela metade. Uma boa noite de sono acalma.
- **Aura espiritual:** começa em 70. Aura baixa faz os espíritos te notarem de longe (×0,5 a ×1,5). Com aura ≥ 70 você resiste a mau-olhado.

## Males (15)
| Mal | Penalidade | Cura (preparo simples) |
|---|---|---|
| Dor de barriga | velocidade −40% | boldo, carqueja |
| Náusea | −15% e passos tortos | boldo, hortelã |
| Ansiedade | sanidade cai 2×, tela treme, recupera pela metade | camomila, folha de maracujá, mulungu, dormir bem |
| Fissura (cigarro) | crises de −15 energia e −8 sanidade, com um tranco curto na tela; **3 doses**, e cada dose espaça as crises (25 s → 50 s → 75 s). No jogo novo a primeira crise só vem **25 s depois do primeiro plantio** (ou no dia 2, se você não plantar) | mulungu (o do kit já está no Livro) |
| Insônia | dormir não recupera | erva-cidreira, maracujá |
| Tosse | espíritos te ouvem de 2× mais longe | guaco |
| Gripe | energia máx. 60 | gengibre |
| Azia | ações custam 2× | espinheira-santa |
| Queimadura | sem caldeirão | babosa |
| Cansaço | velocidade −25% | guaraná (+50% velocidade), erva-mate |
| Dor de dente | vendas −30% | jambu |
| Cólica | velocidade −30% | capim-santo |
| Dor nos rins | velocidade −50% | quebra-pedra |
| Mau-olhado (uruca) | colheita ½, vendas −20%, aura não regenera | arruda, banho de descarrego |
| Assombrado | espíritos até de dia | defumação de guiné, banho |

Ao amanhecer há 50% de chance de surgir um mal novo. Virar a noite acordado dá Cansaço.

## Horta
Preparar a terra → plantar → **regar** → colher. Não há água para buscar: chegue perto do canteiro e aperte **E** (ou **Ação** no iPhone). A rega vale até o fim do dia, e cada canteiro seco mostra o aviso "Regar".

A horta da vó tem **12 canteiros em 3 fileiras de 4**, dentro da cerca ao sul da casa (a porteira fica onde o caminho chega). Cada canteiro é desenhado pelo jogo e muda de cor com o estado: **terra clara com pedrinhas** = bruto (falta preparar), **marrom com sulcos** = preparado, **marrom escuro** = regado. O canteiro que a ação vai atingir ganha uma **moldura amarela pulsando**.

### Pá: canteiros em qualquer lugar
A **pá** custa 25 moedas na feira (aba **Comprar**) e dá para comprar no primeiro dia. Com ela, pare num lugar livre do sítio: aparece a moldura amarela de um canteiro à frente da raizeira e o botão **Cavar canteiro aqui**. Aperte **E** (ou **Ação**) e o canteiro nasce **já preparado**, pronto para plantar. O canteiro cavado é **só para as plantas de proteção**, as que fazem efeito na área (Espada-de-São-Jorge, Pimenteira, Guiné e Comigo-ninguém-pode), postas onde a proteção faz falta, como na porta da casa. As outras sementes aparecem como **Só na horta** e vão na horta da vó, que continua aceitando tudo (`FarmPlotModel.Aceita`). Num save antigo, o que já estava plantado num canteiro cavado continua lá.

| Regra | Valor |
|---|---|
| Energia para cavar | 6 (preparar a terra da horta custa 4) |
| Onde | na grade da horta (1,6 × 1,4), estendida pelo mapa inteiro: os canteiros ficam alinhados e nunca se sobrepõem |
| Onde não | fora do mapa, na mata, no lago, em cima de outro canteiro, da casa, das estações, da feira, dos aldeões e das ervas da mata |
| O que planta | só plantas de proteção (com efeito de área); a horta da vó aceita qualquer uma |
| Limite | 24 canteiros cavados |
| Desfazer | abra um canteiro cavado e vazio e use **Desfazer este canteiro**; os da horta da vó ficam |

A moldura só aparece com a raizeira parada (andando ela some) e quando não há nada por perto para fazer. As regras ficam em `GameSession.DigPlot` / `FillPlot` e `FarmLayout` (`Core/Farming.cs`); o lugar livre é conferido pelo `GameManager.CanDigAt`.

## Nível de raizeira
Começa no nível 1 e vai até o 5 (`Core/Progression.cs`). Cada planta tem um nível mínimo (`"nivel"` no `plants.json`) para **comprar a semente, plantar e coletar na mata**. A feira mostra as sementes trancadas com o nível que falta.

| Ação | Experiência |
|---|---|
| Colher um canteiro | 5 |
| Coletar na mata | 3 |
| Descobrir uma planta (nova página do Livro) | 10 |
| Preparar no caldeirão | 5 |
| Curar um aldeão | 15 |

| Nível | XP total | Libera |
|---|---|---|
| 1 | 0 | boldo, camomila, hortelã, erva-cidreira, arruda, espada-de-são-jorge, manjericão, quebra-pedra |
| 2 | 40 | capim-santo, pimenteira, babosa, alecrim, carqueja, jambu, mulungu |
| 3 | 110 | maracujá, guaco, gengibre, guiné |
| 4 | 220 | espinheira-santa, erva-mate, comigo-ninguém-pode |
| 5 | 380 | guaraná |

No HUD, a barra **XP** mostra o nível e quanto falta para o próximo. Ao subir de nível vem uma festa de uns 6 s (`UI/LevelUpFx.cs`), toda desenhada em código, sem imagens novas:
- **Balões** coloridos sobem do pé da tela balançando no barbante.
- **Fogos de artifício** dos dois lados da faixa: o foguete sobe com rastro, estoura em faíscas que caem e piscam, com som de "fiu… pou" gerado em código.
- **O número cresce como uma planta:** na faixa "SUBIU DE NÍVEL!", o número antigo murcha, fica marrom e afunda na terra. O novo brota do mesmo canteiro: estica, assenta, abre duas folhas e vai de verde-broto a dourado, com uma florada no fim. Dos lados, duas plantas liberadas no nível crescem pelos 5 estágios da arte do jogo (sem planta nova, o mulungu).
- Embaixo, as plantas liberadas; ainda há o brilho em volta da raizeira, a barra de XP piscando e o arpejo.

## Tempo e janelas
O dia dura 240 s. O **Menu** (Esc), os **Jogos salvos**, os **Bastidores** e as telas de abertura e de fim pausam o relógio. Bolsa, Livro, Caldeirão, Varal, Pilão, Feira, plantio e aldeões **não pausam**: o relógio aparece no canto da janela, o varal seca na sua frente e a noite pode chegar (a raizeira só não anda com a janela aberta). Trocar de janela ou minimizar no Mac abre o Menu sozinho; a música continua tocando.

As duas primeiras noites são seguras (a casa ainda está benzida pela vó). Veja [Assombrações](11-assombracoes.md).

## Varal, caldeirão e cliques
- **Varal:** 6 ganchos, 45 s para secar. **Tirar** devolve a erva para a bolsa sem perder nada: seca se já secou, fresca se ainda não. A erva seca serve nos preparos simples do caldeirão como a fresca (o chá de mulungu sai das duas), vale 1,5× na feira e vira pó no pilão para os elixires.
- **Caldeirão e listas:** depois de um clique, os botões das listas ficam travados por 0,3 s (0,8 s no **Preparar**, que mostra "Preparando…"). Um clique duplo não prepara duas vezes nem cai no botão da linha que subiu para o lugar da que sumiu.

## Jogo salvo
O jogo abre no menu inicial: **Continuar** (o save mais recente), **Novo jogo**, **Jogos salvos** (carregar ou apagar) e **Bastidores do desenvolvimento** (como o jogo foi feito; também no Menu da partida). Durante a partida, o **Menu** (Esc ou botão Menu) salva o jogo, e dormir na cama também salva. Na lista, cada partida mostra **quando foi salva** (é a ordem da lista e do Continuar) e quando começou; no menu inicial a primeira linha diz "(é o Continuar)", e durante a partida a atual diz "(esta partida)" e não pode ser apagada. Apagar pede confirmação e manda o arquivo para `saves/lixeira` (guarda os 20 últimos): logo depois aparece **Desfazer**. Durante uma partida, carregar outra também pede confirmação ("Carregar mesmo"), porque o que não foi salvo se perde. Fica salvo: dia e hora, moedas, reputação, experiência, energia, sanidade, aura, males e doses, bênçãos, bolsa, Livro, varal, canteiros (inclusive os cavados com a pá, no lugar deles), a pá, pedidos dos aldeões (e o dia em que chegaram), o Histórico e a posição da personagem. Saves feitos antes da pá continuam abrindo. Assombrações e ervas da mata recomeçam ao carregar.

## Economia
- Preço de venda: erva fresca = base · seca = 1,5× · pó = 2× · produto = preço da receita (preparo simples = 2×base + 2).
- Modificadores: dor de dente ×0,7 · mau-olhado ×0,8 · sorte ×1,3.
- **Feira:** na aba Vender, **Vender 1**, **Vender todos** (de um item) e **Vender tudo** (a bolsa inteira, com confirmação e o total do dia). Na aba Comprar, sementes de uma em uma ou **+5**.
- **Aldeões:** 3 pedidos, cada um com um mal que tem cura possível. Recompensa de 15 a 25 moedas e +1 de reputação. Os pedidos **valem uma semana** (a janela do aldeão diz até que dia) ou **até os três serem atendidos**: na manhã seguinte chegam outros (`GameSession.DiasDosPedidos`, `AtualizarPedidos`). Quem não foi atendido em 7 dias procura outra raizeira, e um aviso explica. Cada um dos 6 aldeões tem a sua arte (`npc_{id}.png`, `Village.Ids`), que segue o nome do pedido e não o lugar na vila.

---

