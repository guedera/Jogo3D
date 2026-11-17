# SkyRacers — Game Design Document

## Estado atual

Atualizado em 17/11/2026 após o primeiro playtest do usuário, que aprovou câmera e dirigibilidade. Revisão alpina implementada em Unity 6000.6.6f1, URP e Input System. Cena: `Assets/Scenes/CanyonRun.unity`. Compilação C# e referências verificadas; a revisão visual e de velocidade ainda requer novo playtest. A build Windows permanece pendente.

## Conceito e escopo

Jogo 3D de corrida aérea solo com uma única fase. O jogador começa pilotando um caça inspirado no F/A-18, já em voo dentro de um cânion, e precisa chegar ao final no menor tempo possível. A dificuldade vem das curvas, da gestão da velocidade e da obrigação de permanecer abaixo do topo do cânion.

Decisões confirmadas:

- Câmera em terceira pessoa atrás do avião.
- Pilotagem semirrealista com resposta arcade: inércia e sensação de voo, com controles acessíveis.
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

Contagem de preparação de 3 segundos com avião e cronômetro parados; teto de voo de 150 unidades acima do relevo local; validação sequencial de setores invisíveis para impedir atalhos. Setores não são pontos de respawn: a derrota reinicia desde o começo. A ponte central exige passagem sob seu arco principal; contorná-la encerra a run.

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

Uma fase fixa de cânion alpino inspirada na atmosfera do voo em cânion de Top Gun: Maverick: rocha cinza, pinheiros, riacho, neve parcial e montanhas ao fundo. Piso e bordas sobem e descem ao longo da fase. A largura foi reduzida em 10% em relação ao primeiro playtest. As árvores são agrupadas em meshes por trecho para limitar a quantidade de renderers.

Há uma única ponte de pedra, com três arcos, aproximadamente no meio do percurso. O arco central tem abertura de cerca de 161 unidades de largura e 89 de altura no centro, antes das margens de segurança do avião. O tabuleiro alcança o teto local e a passagem é validada para impedir desvios laterais. O HUD avisa a aproximação.

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

Mapeamento implementado: `bone7` e `bone8` são estabilizadores, com pitch conjunto e roll diferencial; `bone15` e `bone17` são os lemes. Não há um airbrake dorsal isolado: a frenagem usa deflexões combinadas dos estabilizadores e lemes, além do aumento de arrasto. A animação de demonstração não roda durante o jogo.

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

Menu principal: título, escolha da única fase disponível, controles e sair. Não mostrar fases fictícias bloqueadas.

HUD: tempo atual, recorde, velocidade, potência, margem até o teto e indicador dos airbrakes. Mensagens de baixa velocidade e proximidade do teto devem ser legíveis sem depender apenas de cores.

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

O percurso atual tem aproximadamente 46,3 km e 501 pontos. O piso varia entre aproximadamente -1 e 209 unidades de altitude. O voo inicia 65 unidades acima do piso, a 170 unidades/s (612 km/h), com potência de 62%. A extensão foi aumentada para acomodar a velocidade supersônica. O tempo idealizado na velocidade máxima é de aproximadamente 127 segundos; manobras, aceleração e frenagens aumentam a duração real.

Propulsão: aceleração-base 24, pós-combustão adicional 16 a 100% de potência, arrasto quadrático 0,00030 e arrasto adicional de airbrakes 0,00085. Em reta nivelada, a velocidade de equilíbrio com pós-combustão é aproximadamente 365 m/s (1.315 km/h). O Mach exibido usa referência fixa de 340 m/s para a atmosfera simplificada do jogo. A integração isolada da aceleração prevê aproximadamente 10,6 segundos de 170 m/s até Mach 1 após aplicar potência máxima; isso não inclui o tempo de avanço da manete nem curvas.

As chamas nascem nos centros dos bocais reais (nós 5 e 6), calculados após a escala do modelo. Sem pós-combustão há brilho curto e discreto; a 100%, a pluma se alonga e fica azul. Câmera, taxas de roll/pitch/yaw e assistência angular foram preservadas.

Sombras: perfil PC com distância 600, resolução 4096 e quatro cascatas; Mobile com distância 300, resolução 2048 e duas cascatas. O limite anterior era 50. A nova fase usa uma chave própria de recorde, preservando os dados antigos sem compará-los ao novo percurso.

Validação desta revisão: compilação C#, referências de assets e cálculos de velocidade/escala/abertura da ponte. O validador de cena do Unity inclui cobertura do piso, setores, teto local, rejeição de desvio da ponte e BoxCast sob o arco central; ainda deve ser executado no editor. Visual, shaders, colisões em execução, FPS e build Windows desta revisão não foram testados neste ambiente.

Conforme o enunciado local `Projeto_3D_20262.docx.pdf`: entrega prevista até 25/11/2026; build para pelo menos Windows; repositório com código-fonte e créditos no README; GDD atualizado; página no itch.io vinculada à jam com instruções e imagens/vídeos; confirmar submissões no Blackboard, GitHub e itch.io.

Jam indicada no enunciado: https://itch.io/jam/projeto-jogo-3d-insper-20262

Os quatro eixos da avaliação são controles/câmera, desempenho/colisões, modelos/materiais/texturas e gerenciamento de estados/erros. A validação final deve cobrir os quatro.
