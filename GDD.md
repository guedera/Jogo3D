# SkyRacers — Game Design Document

## Estado atual

Atualizado em 18/11/2026. Implementado em Unity 6000.6.6f1, URP e Input System. A revisão inclui nova física assistida, voo livre, fitas móveis no HUD e ponte com vão alto. Compilação, referências e testes numéricos passaram; validação em Play Mode e build Windows permanecem pendentes.

## Conceito e escopo

Jogo 3D de corrida aérea solo com uma única fase. O jogador começa pilotando um caça inspirado no F/A-18, já em voo dentro de um cânion, e precisa chegar ao final no menor tempo possível. A dificuldade vem das curvas, da gestão da velocidade e da obrigação de permanecer abaixo do topo do cânion.

Decisões confirmadas:

- Câmera em terceira pessoa atrás do avião.
- Pilotagem semirrealista com sustentação dependente de velocidade e ângulo de ataque, arrasto induzido e controle assistido para teclado. Resposta rápida de roll, limite de −5/+10,5 G (envelope de jogo) e estol progressivo.
- Cronômetro crescente e recorde de menor tempo; não há contagem regressiva para derrota.
- Uma run bem executada deve durar aproximadamente 2–3 minutos.
- Modelo do caça fornecido pelo usuário; cenário construído no projeto.
- Superfícies móveis animadas durante o voo. Dobra das asas desativada, mantendo-as estendidas.
- Airbrakes acionados enquanto X estiver pressionado.

## Regras da fase

1. Começar em voo, alinhado com o corredor inicial, com velocidade e potência suficientes para sustentar o avião.
2. Percorrer o cânion na ordem do trajeto até a chegada.
3. Colidir com o chão, paredes ou obstáculos encerra a run.
4. Ultrapassar o teto de altitude do cânion encerra a run.
5. Cruzar a chegada após completar o percurso encerra o cronômetro e mostra a vitória.
6. Salvar o melhor tempo localmente apenas em runs concluídas e válidas.
7. Reiniciar restaura avião, câmera, potência, superfícies móveis, percurso e cronômetro.

Contagem de preparação de 3 segundos com avião e cronômetro parados; teto de voo de 450 unidades acima do relevo local; validação sequencial de setores invisíveis para impedir atalhos. Setores não são pontos de respawn: a derrota reinicia desde o começo. A ponte central exige passagem sob seu arco principal; contorná-la encerra a run.

## Controles

| Tecla | Ação |
| --- | --- |
| Shift | Aumentar gradualmente a potência enquanto pressionado |
| Ctrl | Diminuir gradualmente a potência enquanto pressionado |
| X, segurado | Abrir airbrakes; soltar fecha |
| A / D | Roll para esquerda / direita |
| S / W | Pitch: levantar / abaixar o nariz |
| Q / E | Yaw para esquerda / direita |
| Esc | Pausar e abrir menu; durante a pausa, voltar à run |

A potência permanece no valor selecionado ao soltar Shift/Ctrl, limitada a 0–100%. Inputs opostos simultâneos se anulam. S levanta o nariz como padrão inicial de controle aeronáutico.

## Pilotagem

Usar um Rigidbody e atualizar a física em FixedUpdate. A potência governa o empuxo; a velocidade resulta de empuxo, arrasto, gravidade e sustentação. Aceleração e frenagem são graduais.

- Roll inclina o avião e a direção da sustentação, contribuindo para as curvas.
- Pitch controla a atitude e permite ganhar ou perder altitude.
- Yaw permite ajustes laterais, com autoridade menor que uma curva coordenada.
- Pouca velocidade reduz sustentação e faz o avião perder altitude de maneira progressiva.
- Airbrakes aumentam o arrasto; não param o avião instantaneamente.
- Amortecimento e assistência moderados mantêm a pilotagem acessível.
- Limitar respostas extremas e ajustar os parâmetros por playtest, sem tentar reproduzir um simulador completo.

Definir largura das curvas, velocidade de cruzeiro e extensão do percurso em conjunto. A duração de 2–3 minutos será medida em voo; não será inferida apenas do comprimento do cenário.

## Câmera e leitura espacial

Câmera suavizada atrás e acima do caça, com antecipação na direção do movimento. Acompanhar parcialmente o roll para transmitir inclinação sem tornar o horizonte difícil de ler. Ajustar distância e campo de visão discretamente conforme a velocidade.

Evitar atravessar paredes usando detecção entre o avião e a posição desejada da câmera. Reposicionar imediatamente ao reiniciar, sem interpolar desde o ponto da colisão. Garantir visibilidade das pontas das asas e das próximas curvas.

## Cenário

Uma fase fixa de cânion alpino inspirada na atmosfera do voo em cânion de Top Gun: Maverick: rocha cinza, pinheiros, riacho, neve parcial e montanhas ao fundo. Piso e bordas sobem e descem ao longo da fase. A largura foi reduzida em 60% em relação à primeira versão alpina. As árvores são agrupadas em meshes por trecho para limitar a quantidade de renderers.

Há uma única ponte de pedra, com três arcos, aproximadamente no meio do percurso. O arco central mantém 22 unidades de largura, com abertura de 408 unidades nas laterais e 438 no centro, antes das margens de segurança do avião. O tabuleiro alcança o teto local e a passagem é validada para impedir desvios laterais. A passagem exige alinhamento lateral das asas, com liberdade vertical dentro do vão alto. Pilares esbeltos sustentam o tabuleiro, sem parede maciça fechando o corredor.

Progressão proposta:

| Trecho | Objetivo |
| --- | --- |
| Entrada ampla e quase reta | Aprender potência, atitude e frenagem |
| Curvas abertas alternadas | Introduzir curvas com roll e pitch |
| Curvas em S e estreitamentos | Exigir antecipação e uso dos airbrakes |
| Sequência final | Combinar precisão e velocidade |
| Chegada visível | Encerrar a run sem ambiguidade |

Sem inimigos, armas, multiplayer, decolagem ou pouso. Usar variação de rochas, iluminação e marcas visuais para orientar o jogador. Avisar sobre proximidade do teto usando texto/ícone além de cor.

## Modelo e animações

Asset importado e inspecionado: `low-poly-f18-super-hornet.zip`. Contém glTF, 115 nós, 74 meshes, 9 materiais/texturas e uma animação de demonstração. Total: 5.351 triângulos. Não há skin; as peças são animadas por transforms. O arquivo não contém autor ou licença.

As dimensões da geometria na pose de voo antes do ajuste são aproximadamente 2,798 × 0,655 × 3,568 unidades. O importador centraliza o modelo, orienta o nariz para +Z e ajusta o comprimento do conjunto para 18 unidades. O trem e suas portas usam as poses finais dos canais de animação. As dobradiças `bone4` e `bone6` ficam neutras, mantendo as asas estendidas.

Mapeamento implementado: oito superfícies. Estabilizadores nos nós 34/36; ailerons externos nas meshes 12/28; flaps internos nas meshes 10/22; lemes nas meshes 40/44. Pivôs adicionais nas bordas de dobradiça preservam a pose original e seguem o enflechamento/inclinação das peças. Roll usa deflexão diferencial dos ailerons, flaps e estabilizadores; pitch usa os estabilizadores; yaw usa ambos os lemes. Flaps descem automaticamente em baixa velocidade ou alto ângulo de ataque. X mistura deflexões de frenagem, com saturação a ±35°. As dobradiças de dobra 15/31 não são animadas. Não existem flaps de bordo de ataque nem airbrake dorsal separados no asset; não é criada movimentação fictícia da asa inteira. A animação de demonstração não roda durante o jogo.

Na importação:

1. Conferir formato, materiais, texturas, orientação e dimensões reais da malha.
2. Padronizar o prefab com eixo frontal +Z e superior +Y, preservando o modelo original como filho visual.
3. Medir comprimento e envergadura após a importação; definir escala de jogo e dimensionar o cânion a partir dessas medidas.
4. Identificar cada superfície, seu pivô, eixo local, posição neutra e amplitude segura.
5. Usar referências explícitas às peças no prefab, sem depender de nomes presumidos.
6. Criar colisores simplificados cobrindo fuselagem e asas, com detecção contínua adequada à velocidade.
7. Preservar asas estendidas e trem de pouso recolhido, caso exista no modelo.

Mapeamento visual pretendido, sujeito à estrutura real do asset:

| Comando | Movimento visual |
| --- | --- |
| Roll | Deflexão diferencial das superfícies disponíveis para rolagem |
| Pitch | Movimento conjunto dos estabilizadores/elevadores disponíveis |
| Yaw | Deflexão dos lemes |
| Airbrakes | Abertura das superfícies de frenagem presentes no asset |
| Potência | Intensidade dos efeitos de motor e do áudio |

As deflexões devem interpolar suavemente e voltar ao neutro. Não animar a dobra das asas. Se o modelo não tiver um airbrake separado, definir uma representação compatível com suas superfícies antes de implementar.

## Interface e estados

Menu principal: título, CANYON 01, VOO LIVRE, controles e sair. Voo livre inicia imediatamente acima do cânion, mantém colisões e física, mas desativa teto/alarme, percurso obrigatório, cronômetro, chegada e recorde. Reiniciar preserva o modo escolhido. Não mostrar fases fictícias bloqueadas.

HUD: instrumentos laterais verde neon de velocidade indicada em km/h, Mach, altitude acima do piso (AGL) e potência. As fitas de velocidade/altitude deslizam continuamente sob índices fixos, com recorte das bordas. A distância restante ao longo do percurso aparece na lateral no modo corrida. Sem mira, mensagens ou cronômetro no centro. Contagem inicial na lateral. Cronômetro permanece ativo e é mostrado no resultado. Alerta de altitude sonoro e indicação lateral intermitente nos últimos 60 metros até o teto.

Estados: Menu Principal, Preparação, Correndo, Pausado, Derrota e Vitória.

- Pausa congela física e cronômetro e oferece Continuar, Reiniciar e Menu Principal.
- Derrota informa o motivo e oferece Reiniciar e Menu Principal.
- Vitória mostra tempo final, recorde e opção de tentar novamente.
- Perda de foco durante a run pausa o jogo para evitar colisões enquanto a janela está inativa.
- Reinício e troca de menu restauram a escala de tempo corretamente.
- Impedir que colisão, teto e chegada encerrem a mesma run mais de uma vez; derrota tem prioridade se houver conflito na mesma etapa de física.

## Organização prevista

| Componente | Responsabilidade |
| --- | --- |
| FlightInput | Ler teclado pelo Input System |
| FlightController | Empuxo, arrasto, sustentação e rotação |
| AircraftVisuals | Superfícies móveis e efeitos do motor |
| ChaseCamera | Acompanhamento e prevenção de obstruções |
| CanyonCourse | Geometria, setores e limites da fase |
| RunManager | Estados, cronômetro, chegada, derrota e reinício |
| GameUI | Menus, HUD e mensagens |
| RecordStore | Persistência local do melhor tempo por fase |

Separar scripts próprios, prefabs, materiais, áudio e cenas de assets de terceiros. Manter o modelo original intacto e configurar um prefab próprio do jogador.

## Ordem de execução

1. Inspecionar e importar o caça; acertar materiais URP, escala, orientação, superfícies e colisores.
2. Implementar voo e câmera em um corredor simples; ajustar a sensação de controle.
3. Construir o cânion fixo com progressão de dificuldade e chegada.
4. Integrar estados, cronômetro, recorde, HUD e menus.
5. Integrar animações, efeitos, áudio e acabamento visual.
6. Validar a run inteira, corrigir colisões e calibrar duração/dificuldade.
7. Gerar build Windows, completar créditos e preparar a entrega.

## Critérios de conclusão

Situação da implementação: os sistemas abaixo estão escritos, com compilação C# verificada. A confirmação em Play Mode permanece pendente.

- Todos os comandos funcionam conforme a tabela, inclusive teclas pressionadas simultaneamente.
- A câmera mantém boa visibilidade em curvas e perto de paredes.
- Paredes e piso não podem ser atravessados na maior velocidade atingível.
- O teto encerra a run no limite visual esperado.
- A chegada só aceita percurso válido e grava recorde apenas se o tempo melhorar.
- Pausar não acumula tempo; reiniciar zera tudo; voltar ao menu permite jogar novamente.
- As superfícies móveis seguem os comandos, sem dobrar as asas.
- Uma run representativa dura aproximadamente 2–3 minutos.
- Jogo utilizável do menu à vitória/derrota e de volta, sem erros no Console.
- Build Windows testada; desempenho medido no hardware de teste, buscando 60 FPS com qualidade adequada.

## Entrega acadêmica

A cena inicial monta o jogo ao dar Play, sem importador externo. O menu `SkyRacers > 1 - Build Game Scene` permite gerar a versão editável com meshes e prefab persistentes. O comando de build Windows está no mesmo menu.

O percurso atual tem aproximadamente 46,3 km e 1.001 pontos. O piso varia entre aproximadamente -1 e 209 unidades de altitude. A corrida inicia 65 unidades acima do piso, a 170 unidades/s (612 km/h), com potência de 62%. O voo livre inicia 595 unidades acima do piso no mesmo ponto de partida. A duração com a nova física deve ser medida em voo.

Propulsão: aceleração-base 18 m/s² e pós-combustão adicional 10 m/s² a 100% de potência. O Mach usa referência fixa de 340 m/s. Massa de 12.000 kg, área de referência de 37,2 m² e densidade decrescente com altitude. Sustentação por ângulo de ataque e pressão dinâmica; arrasto de perfil, induzido, transônico, de derrapagem e dos airbrakes. W/S comandam taxa de pitch de até 75°/s, com proteção preditiva de ângulo de ataque e carga G. Ao soltar, o trim estabiliza o voo. Aceleração angular limitada a 480°/s² no pitch; roll até 150°/s. Pose usada na física vem de Rigidbody.rotation, sem atraso da interpolação visual. A gravidade e a orientação da sustentação produzem as curvas, sem realinhamento artificial da velocidade. Parâmetros ajustados para jogabilidade, não dados certificados do F/A-18.

O ensaio longitudinal de 20 s mantém voo neutro com desvio vertical de aproximadamente −5,7 m. O ensaio de 2 s puxando o nariz a plena potência altera a atitude em cerca de 75°. Com pós-combustão, o ensaio de 20 s chega a Mach 1,29. Esses testes exercitam as equações reais, mas não substituem a execução do Rigidbody, das colisões e dos controles no Unity.

As chamas nascem nos centros dos bocais reais (nós 5 e 6), calculados após a escala do modelo. Sem pós-combustão há brilho curto e discreto; a 100%, a pluma se alonga e fica azul. A câmera foi preservada; taxas e controle angular foram revisados para a nova física.

Sombras: perfil PC com distância 600, resolução 4096 e quatro cascatas; Mobile com distância 300, resolução 2048 e duas cascatas. O limite anterior era 50. A nova fase usa uma chave própria de recorde, preservando os dados antigos sem compará-los ao novo percurso.

Validação desta revisão: compilação C#, referências de assets e cálculos de velocidade/escala/abertura da ponte. O validador de cena do Unity inclui cobertura do piso, setores, teto local, rejeição de desvio da ponte e BoxCast sob o arco central; ainda deve ser executado no editor. Visual, shaders, colisões em execução, FPS e build Windows desta revisão não foram testados neste ambiente.

Conforme o enunciado local `Projeto_3D_20262.docx.pdf`: entrega prevista até 25/11/2026; build para pelo menos Windows; repositório com código-fonte e créditos no README; GDD atualizado; página no itch.io vinculada à jam com instruções e imagens/vídeos; confirmar submissões no Blackboard, GitHub e itch.io.

Jam indicada no enunciado: https://itch.io/jam/projeto-jogo-3d-insper-20262

Os quatro eixos da avaliação são controles/câmera, desempenho/colisões, modelos/materiais/texturas e gerenciamento de estados/erros. A validação final deve cobrir os quatro.


## Revisão de efeitos e cenário — 18/11/2026

- Profundidade triplicada para 450 m, largura reduzida para aproximadamente 59–101 m no fundo. Postes removidos; chegada marcada no piso.
- Duas cachoeiras com névoa, relevo com mais segmentos, normais suavizadas e detalhe procedural na superfície para reduzir o aspecto facetado.
- Arco central de 22 × 438 m, com pilares altos e cabeceira próxima ao tabuleiro. A passagem exige alinhamento lateral.
- HUD lateral verde neon de altitude, velocidade e potência; centro da tela livre. Alarme pulsado de altitude que cessa em pausa, derrota ou retorno à altitude segura.
- Fumaça dos motores e trilhas nas pontas das asas com baixa opacidade.
- Impacto separa as 74 meshes em destroços com colisores, gera fogo, fumaça, fragmentos e som; aguarda 2,5 s antes do resultado. Reiniciar restaura o avião e remove destroços.
- Boom sônico e anel discreto de condensação somente na passagem de Mach < 1 para Mach >= 1 durante a run. Não dispara ao desacelerar, permanecer supersônico ou retomar de uma pausa.
- Nova física assistida e modo livre para testes. Novo identificador de recorde para não misturar tempos da física anterior.

Compilação C# e referências verificadas. Efeitos, som, desempenho e passagem pela ponte ainda precisam de playtest no Unity nesta revisão.
