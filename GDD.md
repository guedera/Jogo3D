# SkyRacers — Game Design Document

Atualizado em 24/11/2026. Este documento descreve a implementação atual do projeto Unity 6000.6.6f1, com URP e Input System.

## Conceito e experiência

SkyRacers é um jogo 3D solo de corrida aérea e evasão, inspirado no voo em cânion de Top Gun: Maverick. O jogador pilota um caça inspirado no F/A-18, atravessa um cânion alpino e retorna à base no menor tempo possível. Curvas, obstáculos, gestão de energia e exposição à defesa antiaérea formam o desafio.

O escopo inclui uma missão de ida e volta e um mapa independente de voo livre. Não há multiplayer, armas ofensivas do jogador, decolagem ou pouso. A pilotagem combina forças aerodinâmicas com assistência para teclado e controle; os parâmetros priorizam o jogo e não representam uma simulação certificada do F/A-18.

## Objetivos e regras da missão

1. O caça começa alinhado com o cânion, 65 m acima do piso, a 170 m/s (612 km/h), com potência de 62%.
2. Uma preparação de três segundos antecede o movimento e o cronômetro.
3. O jogador percorre os setores na ordem e atravessa o arco central da ponte no meio da fase.
4. Passar por dentro do arco de RETORNO, no último setor, inicia a volta. O jogador faz a curva manualmente na bacia de manobra; não há teleporte nem vitória nesse ponto.
5. Na volta, percorre os setores na ordem inversa e atravessa novamente a ponte central.
6. Passar por dentro do arco da BASE, no primeiro setor, após completar o trajeto, encerra o cronômetro e concede vitória.
7. Colisão com cenário ou impacto de míssil encerra a run. Contornar a passagem central da ponte também causa derrota.
8. Subir acima de 450 m sobre o piso local não causa derrota automática: permite aquisição e disparos de SAMs tanto na ida quanto na volta.

O percurso tem 1.001 pontos e aproximadamente 46,3 km por perna, totalizando 92,6 km de ida e volta, sem contar desvios e a manobra de retorno. A distância do HUD acompanha o progresso pelos setores. Não há prazo fixo para derrota: vence-se completando a missão, e o menor tempo é salvo localmente.

Setores não são checkpoints de respawn. Reiniciar restaura o início da missão, cronômetro, potência, avião, câmera e dez cargas de flares; remove projéteis, contramedidas e destroços. Passar acima ou ao lado dos arcos de missão não ativa o marco nem causa derrota automática: o jogador deve reposicionar e cruzar corretamente.

## Cenário e marcos

O cânion tem rocha cinza, pinheiros, neve parcial, montanhas, duas cachoeiras com névoa e variações de elevação. O piso varia aproximadamente entre −1 e 209 m de altitude absoluta. O corredor é profundo e estreito: referência de 450 m de profundidade e cerca de 59–101 m de largura no fundo, exceto a região de retorno. O riacho foi removido.

| Marco | Geometria e função |
| --- | --- |
| Arco da BASE | Vão de 64 m de largura, laterais abertas até 110 m e topo da abertura a 150 m; encerra a missão ao voltar |
| Ponte central | Única ponte de pedra, com três arcos; passagem obrigatória pelo central, de 22 m de largura e abertura entre 408 e 438 m de altura |
| Arco de RETORNO | Vão de 180 m de largura, laterais abertas até 180 m e topo da abertura a 240 m; inicia a volta |
| Bacia final | Alargamento progressivo até 6,4 km e cerca de 5 km extras de terreno longitudinal para permitir a curva |

A validação da ponte usa a abertura real. Asas e fuselagem são tratadas pelos colisores do avião, sem a antiga margem fixa de 8 m por lado que gerava derrotas indevidas. O tabuleiro fica a aproximadamente 458 m sobre o piso local.

O terreno externo alcança aproximadamente 48 km de largura e possui 16 km adicionais nas duas extremidades do terreno anterior. As malhas externas compartilham as bordas do cânion para fechar o entorno sem alargar o corredor. O mundo permanece finito.

## Defesa antiaérea

SAMs podem atacar nas duas pernas da missão. O modo livre não possui defesa antiaérea.

| Parâmetro | Valor atual |
| --- | --- |
| Exposição | Ponto mais alto do avião acima de 450 m sobre o piso local |
| Alcance de aquisição | Até 6,5 km da bateria, com visão direta |
| Tempo de aquisição | 1,5 s contínuo de exposição válida |
| Intervalo entre disparos | Pelo menos 4 s |
| Máximo simultâneo | Quatro mísseis ativos |
| Velocidade inicial / máxima | 300 / 950 m/s |
| Aceleração | 240 m/s² |
| Agilidade | Até 32 G e 100°/s; raio de curva aumenta com a velocidade |
| Perda de rastreamento por relevo | Mais de 0,8 s de visão bloqueada |
| Tempo máximo de voo | 24 s |

Descer ou perder a linha de visão interrompe novas aquisições. Mísseis já lançados continuam em voo; não desaparecem ao entrar no cânion. Podem atingir o terreno, ser desviados pelas contramedidas ou perder rastreamento. A colisão considera o deslocamento do míssil e do caça entre passos de física para reduzir atravessamentos em alta velocidade.

Fumaça espessa e pluma visível ajudam a localizar a ameaça. O HUD lista distância em metros e direção relativa de cada míssil (frente, atrás, esquerda ou direita); o alerta principal destaca o mais próximo. O alarme de míssil tem prioridade sobre o de altitude.

## Flares

O estoque é de **dez cargas**, e não dez partículas individuais. Cada pressionamento libera uma salva de 24 flares, distribuídos em 12 pares bilaterais durante 0,55 s. O intervalo mínimo entre cargas é 0,6 s; segurar o botão não consome cargas continuamente.

Os flares se abrem em leque a partir da posição atual do caça, com núcleo branco/dourado, rastro quente e fumaça. Um flare recente (menos de 2,5 s), visível e dentro do cone frontal do míssil pode desviá-lo entre 90 e 850 m de distância. Acionar tarde demais pode falhar. Reinício recupera dez cargas; pausa congela a sequência e a movimentação. Também é possível soltá-los no voo livre.

Referência cinematográfica: [roteiro de Top Gun: Maverick](https://assets.scriptslug.com/live/pdf/scripts/top-gun-maverick-2022.pdf), especialmente pp. 72, 95 e 137. O jogo adota proteção pelo relevo, exposição na subida e evasão com recursos limitados. Os valores de cadência, velocidade e guiagem são ajustes próprios; “flare” representa a salva cinematográfica de contramedidas, sem reproduzir sensores reais.

## Voo livre — Granite Range

Mapa independente de 20 × 20 km com montanhas, florestas, 24 formações rochosas e sete arcos para treinar manobras. Começa imediatamente em (x = 0, z = −7500), 420 m acima do terreno, com a mesma física da missão.

Não possui teto de altitude, SAMs, setores obrigatórios, cronômetro ou recorde. Colisões permanecem ativas. O terreno é finito, mas sair de seus limites não encerra automaticamente o voo. Reiniciar mantém o modo livre. A troca de modo ativa apenas o cenário correspondente.

## Controles

| Ação | Teclado | Controle PlayStation / Xbox |
| --- | --- | --- |
| Aumentar / diminuir potência | Shift / Ctrl, segurados | R2 / L2 ou RT / LT |
| Airbrakes | X, segurado | X / A, segurado |
| Roll | A / D | Analógico esquerdo horizontal |
| Pitch | S sobe, W desce | Analógico esquerdo: puxar sobe, empurrar desce |
| Yaw / leme | Q / E | L1 / R1 ou LB / RB |
| Uma carga de flares | F | Círculo / B |
| Câmera orbital | Setas | Analógico direito |
| Pausa / continuar | Esc | Options / Start |
| Navegação no menu | Setas ou mouse | D-pad ou analógico esquerdo |
| Confirmar no menu | Enter ou clique | X / A |

Potência permanece no valor escolhido ao soltar teclas/gatilhos, entre 0 e 100%. Comandos opostos se anulam. Para uma curva coordenada, inclinar com roll e puxar pitch; yaw permite ajustes laterais.

## Física e modelo

Rigidbody atualizado em FixedUpdate, com passo de 0,02 s. Empuxo, arrasto, gravidade e sustentação determinam a velocidade. Sustentação depende de densidade, velocidade e ângulo de ataque. Há arrasto induzido, transônico, de derrapagem e dos airbrakes; estol progressivo, trim e proteção de ângulo de ataque/carga G.

| Parâmetro | Valor |
| --- | --- |
| Massa / área de referência | 12.000 kg / 37,2 m² |
| Aceleração-base do motor | 18 m/s² |
| Pós-combustão | Mais 10 m/s² a 100% de potência |
| Referência de Mach | 340 m/s |
| Envelope do caça | −5 a +10,5 G |
| Taxas máximas comandadas | Pitch 75°/s, roll 150°/s, yaw 24°/s |
| Aceleração angular de pitch | 480°/s² |
| Transição angular | 35 ms para pequena sensação de inércia |

O modelo fornecido possui 115 nós, 74 meshes, 9 materiais/texturas e 5.351 triângulos. Foi ajustado para 18 m de comprimento, nariz em +Z e aproximadamente 14 m de envergadura. Trem e portas permanecem recolhidos. A animação de demonstração não roda durante o jogo.

Oito superfícies são animadas: estabilizadores (nós 34/36), ailerons (12/28), flaps (10/22) e lemes (40/44). Pivôs seguem as dobradiças do asset. Flaps também respondem automaticamente à velocidade/ângulo de ataque; a frenagem mistura deflexões com saturação a ±35°. Dobra das asas (15/31) permanece desativada. Não há airbrake dorsal nem flaps de bordo de ataque separados no asset.

## Câmera, interface e efeitos visuais

Câmera traseira suavizada, com antecipação, acompanhamento parcial do roll e detecção de paredes. Setas/analógico direito permitem órbita; após dez segundos sem comando de câmera, ela recentraliza suavemente. Pausa congela a espera, e reinício centraliza imediatamente.

Menu como painel de operações, com mapa vetorial, briefing, controles e seleção entre missão e voo livre. HUD lateral verde neon, com fitas móveis de velocidade indicada e altitude AGL, potência, Mach, distância restante, perna atual, rumo, tempo e estoque de cargas. O centro permanece livre de mira e instrumentos. Alertas de altitude aparecem nos últimos 60 m antes do teto.

| Efeito | Implementação atual |
| --- | --- |
| Fog | Linear, inicia a 700 m e encobre completamente a geometria a 2.700 m, antes do corte da câmera a pelo menos 3.500 m |
| Nuvens | Grupos de partículas estáticas suaves, sem colisão, nos dois mapas; maiores e mais opacas, com um banco à frente do spawn |
| Vinheta | Intensidade 0,30, suavidade 0,55 |
| Aberração cromática | Intensidade 0,24 |
| Pós-combustão | Chama azul ancorada nos bocais reais, intensificada a 100% |
| Escapamento / asas | Fumaça discreta atrás dos motores e trilhas finas nas pontas das asas |
| Boom sônico | Som e três anéis expansivos de condensação formando um cone curto, com 240 partículas e duração aproximada de 0,5–0,64 s |
| Colisão | Separação das 74 meshes em destroços com colisores, fogo, fumaça, fragmentos e som; resultado após 2,5 s |

Boom sônico dispara somente ao cruzar de Mach < 1 para Mach ≥ 1 durante o voo ativo; não dispara ao desacelerar ou permanecer supersônico. HUD em overlay não recebe os efeitos de lente.

Sombras PC: distância 600 m, atlas 4096 e quatro cascatas. Mobile: 300 m, atlas 2048 e duas cascatas. Florestas são agrupadas por trecho para reduzir renderers. O custo gráfico deve ser medido por plataforma.

## Áudio e música

Motor, alarmes, impacto e boom sônico usam áudio procedural. O alerta de míssil tem tom mais alto que o de altitude.

| Contexto | Música |
| --- | --- |
| Menu principal | Top Gun Anthem — Harold Faltermeyer |
| Início da missão do cânion, incluindo preparação | Danger Zone — Kenny Loggins |
| Voo livre | Sem música de fundo |

Arquivos fornecidos pelo usuário, convertidos para OGG em `Assets/Resources/Music/Menu.ogg` e `Canyon.ogg`. O sistema usa repetição, áudio 2D e entrada suave até volume 0,28. Pausa suspende a música e continuar retoma do mesmo ponto. Reiniciar a missão reinicia a faixa; voltar ao menu reinicia a música do menu. A faixa da missão continua nas telas de resultado até trocar de contexto.

## Estados e persistência

Estados: Menu, Preparação, Correndo, Pausado, Derrota e Vitória. Pausa congela física, cronômetro, mísseis, contramedidas e música. Oferece continuar, reiniciar e menu principal. Perder o foco durante a run também pausa.

Vitória mostra tempo, melhor tempo e indicação de novo recorde. Derrota informa motivo. A chave de recorde da missão de retorno é separada das versões anteriores. Voo livre nunca registra tempos. Transições e reinícios restauram a escala de tempo e limpam objetos temporários.

## Organização técnica

| Componente | Responsabilidade |
| --- | --- |
| GameBootstrap / SkyRacersBuilder | Montagem em execução ou geração de cena editável/build |
| FlightInput | Teclado e gamepad |
| FlightController / FlightDynamics | Rigidbody e equações de voo |
| AircraftFactory / AircraftVisuals / FlightSurfaceMixer | Modelo e superfícies móveis |
| FlightEffects | Motor visual, alarmes, boom sônico e destroços |
| ChaseCamera | Câmera, lente e fog em execução |
| CanyonCourse / AlpineMesh | Traçado, terreno, ponte e arcos de missão |
| FreeFlightWorld / SimpleClouds | Mapa de treino e nuvens |
| RunManager / MissionProgress | Estados e sequência de ida/volta |
| AirDefense / MissionRules | SAMs, guiagem e contramedidas |
| GameUI / HudTape / MissionMapGraphic | Menus, instrumentos e mapas |
| GameMusic | Troca de faixas, entrada suave e pausa |
| RecordStore | Melhor tempo local |

A cena `Assets/Scenes/CanyonRun.unity` monta o jogo ao iniciar. O comando `SkyRacers > 1 - Build Game Scene` gera uma versão editável e sobrescreve essa cena; edições manuais devem ser preservadas separadamente. O menu também oferece abertura da cena, build Windows e validação geométrica do percurso.

## Distribuição e créditos

Página do projeto: https://guedera.itch.io/skyracers. Uma build WebGL foi enviada e executada pelo usuário. A capa de 630 × 500 mostra um caça frontal em estilo anime; é uma ilustração gerada por IA, não uma captura do gameplay. A página inclui descrição, controles e declaração de uso de IA.

Para atualizar a versão publicada, é necessário recompilar o projeto e substituir o ZIP no itch.io. A configuração de incorporação deve acomodar o canvas; foi relatado recorte na página, com orientação para testar lançamento em tela cheia. O resultado dessa correção não foi confirmado nesta revisão.

Créditos: modelo/texturas do ZIP `low-poly-f18-super-hornet.zip`, fornecido pelo usuário; o ZIP não informa autor/licença. Músicas: Top Gun Anthem, Harold Faltermeyer, e Danger Zone, Kenny Loggins, fornecidas pelo usuário. Cenário, interface, código e áudio procedural desenvolvidos no projeto com assistência de IA. Autorizações/licenças dos assets de terceiros não foram documentadas no projeto.

## Validação e entrega

O usuário realizou playtests e forneceu feedback de pilotagem, cenário, efeitos e publicação WebGL. A compilação C# dos scripts de jogo/editor, referências dos assets e ensaios numéricos automatizados passaram nas últimas verificações. Isso não equivale à validação visual de cada revisão nem à execução de todas as plataformas.

`Tools/verify_project.py` verifica o modelo, superfícies, referências e compilação, além de executar `Tools/FlightChecks.cs`. Os ensaios cobrem dinâmica de voo, inércia, comandos, HUD, sequência de ida/volta, vitória na base, radar, cadência, estoque e velocidade/agilidade dos mísseis. Exemplo: perseguição retilínea de um alvo a 450 m/s, começando 2 km atrás, fecha a distância em 5,76 s. O ensaio isolado de voo a plena potência atinge Mach 1,29 em 20 s; não é uma medição de gameplay.

Validação final necessária:

- Completar ida e volta passando pelos três marcos, sem derrota indevida no arco central.
- Confirmar aquisição de SAMs acima do teto nas duas pernas e ausência de SAMs no voo livre.
- Conferir as dez cargas, as salvas de 24 flares, distância/direção dos mísseis e evasão.
- Testar gamepad físico, pausa, retomada, reinício, troca de modo e recentralização da câmera.
- Conferir fog, nuvens, lente, boom sônico, músicas e volumes no Editor e na nova build WebGL.
- Confirmar menu/HUD sem cortes na página e em tela cheia; áudio após interação no navegador.
- Medir desempenho e colisões em alta velocidade; meta de 60 FPS no hardware de teste.
- Validar a build Windows e as submissões acadêmicas; não há confirmação automatizada de entrega.

Conforme o enunciado local `Projeto_3D_20262.docx.pdf`: entrega prevista até 25/11/2026, build para pelo menos Windows, repositório com código-fonte e créditos, GDD atualizado e página no itch.io vinculada à jam com instruções e imagens/vídeos. Confirmar envios no Blackboard, GitHub e itch.io.

Jam: https://itch.io/jam/projeto-jogo-3d-insper-20262. Os eixos de avaliação são controles/câmera, desempenho/colisões, modelos/materiais/texturas e gerenciamento de estados/erros.
