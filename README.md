# SkyRacers

Corrida aérea solo em um F-18: atravesse o cânion, cruze a ponte, faça a curva no marco final e retorne à base. Na volta, voar acima das bordas expõe o caça aos mísseis terra-ar. Complete ida e volta no menor tempo possível.

## Jogar no Unity

1. Abra `SkyRacers/` no **Unity 6000.6.6f1** e aguarde a importação/compilação.
2. Abra **Assets/Scenes/CanyonRun.unity**.
3. Aperte **Play** e escolha **OPERAÇÃO CÂNION** ou **VOO LIVRE**.

A cena inicial constrói o cenário e o avião ao iniciar; não exige importador glTF nem configuração manual. O menu tem um painel de operações com mapa do percurso, briefing e controles. Mouse, teclado e gamepad navegam nos menus.

**Voo livre — Granite Range:** mapa independente de 20 × 20 km, com montanhas, florestas, 24 formações rochosas e sete arcos de treino. Começa imediatamente, a 420 m sobre o terreno, sem teto, mísseis, cronômetro ou percurso obrigatório. Colisões permanecem ativas. O terreno tem extensão finita, mas sair dele não encerra o voo.

## Controles

| Ação | Teclado | PlayStation / Xbox |
| --- | --- | --- |
| Aumentar / diminuir potência | Shift / Ctrl, segurados | R2 / L2 · RT / LT |
| Airbrakes | X, segurado | X / A, segurado |
| Roll | A / D | Analógico esquerdo, horizontal |
| Pitch | S sobe / W desce | Analógico esquerdo: puxar sobe, empurrar desce |
| Yaw / leme | Q / E | L1 / R1 · LB / RB |
| Olhar ao redor | Setas | Analógico direito |
| Soltar um flare | F | Círculo / B |
| Pausar / continuar | Esc | Options / Start |
| Navegar no menu | Setas ou mouse | D-pad ou analógico esquerdo |
| Confirmar no menu | Enter ou clique | X / A |

A potência permanece no valor escolhido ao soltar os gatilhos/teclas. A câmera volta suavemente ao centro após **10 segundos** sem comando de câmera; pausa não conta nesse intervalo. Cada acionamento emite uma salva de **24 flares** e consome uma das **10 cargas**, com intervalo mínimo de 0,6 s. A salva libera 12 pares em leque ao longo de 0,55 s, com núcleos luminosos e fumaça. Segurar o botão não consome novas cargas.

## Missão de ida e volta

- Preparação de três segundos; cronômetro único para as duas pernas, aproximadamente 92,6 km ao todo.
- Setores obrigatórios na ordem de ida e na ordem inversa na volta. É necessário passar sob o arco central da única ponte nos dois sentidos.
- Chegar ao marco final muda o objetivo para **RETORNO À BASE**. O piloto faz a curva; não há teleporte. O fim do cânion tem uma bacia larga com terreno adicional para a manobra.
- Ultrapassar 450 m acima do piso local **não causa derrota automática**. Na ida há aviso de altitude. Na volta, o caça fica exposto ao radar.
- Bater ou contornar a passagem central da ponte encerra a run. Vitória e recorde só ocorrem ao completar o retorno ao início.
- Pausa congela física, mísseis, contramedidas e cronômetro. Reiniciar restaura avião, câmera, dez flares e missão; elimina projéteis e destroços. Perder o foco da janela pausa o voo.

### Mísseis e contramedidas

Baterias no alto do cânion só lançam na volta, acima do teto, com visão direta e a até 6,5 km. A primeira aquisição exige 1,5 s contínuo de exposição. Novos disparos têm intervalo mínimo de 4 s; há no máximo quatro mísseis ativos. Descer ou perder a linha de visão interrompe novas aquisições.

Mísseis já lançados continuam em voo: não desaparecem ao descer. Seu giro é limitado ao envelope positivo de 10,5 G usado pelo caça, com teto de 75°/s; aceleram de 300 até 950 m/s. Bloqueio de visão pelo relevo por mais de 0,8 s elimina o rastreamento. Colidem com o terreno e expiram após 24 s.

Flares recentes, visíveis e à frente do míssil podem desviá-lo entre 90 e 850 m de distância. Disparar tarde demais pode falhar. HUD lateral mostra estoque, aquisição e mísseis em voo; o alerta sonoro muda quando há ameaça.

Inspiração pesquisada: [roteiro de Top Gun: Maverick](https://assets.scriptslug.com/live/pdf/scripts/top-gun-maverick-2022.pdf), pp. 72, 95 e 137: proteção pelo cânion, exposição durante a subida e evasão com contramedidas limitadas. Esses princípios orientam o jogo. Cadência, velocidades e janelas de reação são ajustes próprios, pois o filme não define esses parâmetros. “Flare” representa a salva cinematográfica de contramedidas (flare/chaff), não uma simulação de um sensor real de míssil.

## Voo, cenário e efeitos

- Física com sustentação por velocidade, densidade e ângulo de ataque; arrasto induzido, estol progressivo, trim e proteção de carga G. Mantida a pilotagem aprovada: pitch até 75°/s, roll 150°/s, envelope −5/+10,5 G e transição angular de 35 ms.
- F-18 texturizado com oito superfícies animadas: estabilizadores, ailerons, flaps e lemes. Asas permanecem estendidas e trem recolhido.
- Pós-combustão a 100% com chama azul nos bocais; fumaça e trilhas de ponta de asa; boom sônico apenas ao cruzar Mach 1 acelerando.
- Cânion alpino de 450 m de profundidade, pinheiros, neve, relevo variável e duas cachoeiras. **Riacho removido.** Ponte central com vão de 22 m de largura e até 438 m de altura.
- HUD lateral verde, fitas móveis de velocidade/altitude, potência, Mach, distância total restante, perna da missão, rumo ao percurso, tempo e flares. Centro livre.
- Colisões desmembram as 74 meshes do avião, com explosão, fumaça e som; resultado aparece após 2,5 s.
- Sombras: perfil PC com distância 600, atlas 4096 e quatro cascatas; Mobile com 300, atlas 2048 e duas cascatas.

## Editar e gerar build

| Menu SkyRacers | Uso |
| --- | --- |
| 1 - Build Game Scene | Gera prefab e meshes persistentes e substitui CanyonRun por uma versão editável |
| 2 - Open Game Scene | Abre a cena do jogo |
| 3 - Build Windows | Gera `SkyRacers/Builds/Windows/SkyRacers.exe` |
| 4 - Validate Course | Verifica piso, setores nos dois sentidos, travessia acima do teto e ponte; usar na cena gerada ou durante Play com o cânion ativo |

O comando 1 reconstrói a cena: salve em outra cena qualquer edição manual que queira preservar. É opcional; a cena inicial já executa e compila diretamente. O mapa de treino é construído separadamente ao entrar no voo livre.

Parâmetros: `FlightDynamics`, `CanyonCourse`, `FreeFlightWorld`, `MissionRules` e `AirDefense`. Planejamento: [GDD.md](GDD.md).

## Verificação — 24/11/2026

`python3 Tools/verify_project.py` compila scripts de jogo/editor contra as assemblies instaladas do Unity, valida referências/modelo e executa ensaios numéricos das equações reais. Requer uma primeira importação do projeto no Unity macOS.

Passaram: compilação C#, dados do avião, referências, dinâmica de voo, superfícies, HUD, sequência ida/volta, vitória apenas na base, condições de aquisição, cadência de lançamento e limite/reset dos dez flares. O ensaio longitudinal isolado continua chegando a Mach 1,29 em 20 s de potência máxima, partindo de 170 m/s a 600 m.

**Ainda precisam de Play Mode:** aparência do menu/efeitos, gamepad físico, câmera orbital, colisões/homing no cenário, desempenho e build Windows. A execução adicional do Unity neste ambiente é bloqueada pelos serviços locais do editor; compilação e ensaios numéricos não substituem esses testes.

Roteiro de playtest:

1. Navegar pelo menu com mouse, teclado e controle. Conferir o mapa/briefing de cada modo.
2. Entrar em voo livre: verificar terreno independente, arcos, colisões e ausência de teto/SAM. Reiniciar deve manter esse modo.
3. Conferir todos os eixos/gatilhos, airbrakes e flares. Girar a câmera, esperar 9 s (mantém posição), depois mais 1 s (começa a voltar); pausa deve congelar a espera.
4. Soltar dez flares: estoque termina em zero e o 11º comando não emite. Reiniciar restaura dez. Segurar o botão emite apenas uma salva de 24 flares.
5. Na missão, subir acima do teto na ida: aviso, sem derrota nem lançamento. Completar a ida: objetivo muda, sem vitória; virar e retornar pelo percurso.
6. Na volta, expor-se acima do teto por mais de 1,5 s: conferir radar e disparos periódicos. Descer interrompe novos disparos, mas mantém os já lançados. Testar evasão usando relevo e flares.
7. Cruzar a ponte pelo arco central nos dois sentidos. Retornar à base para vencer. Distância deve cair ao longo das duas pernas; somente missão completa grava recorde.
8. Pausar durante perseguição; continuar sem saltos. Reiniciar/voltar ao menu deve limpar mísseis, flares e destroços. Repetir a troca entre modos e conferir Console/FPS.

`Tools/convert_aircraft.py` regenera dados do modelo. `Tools/create_bootstrap_scene.py` sobrescreve a cena inicial e materiais; não é necessário para esta atualização.

## Assets e créditos

| Recurso | Origem |
| --- | --- |
| Modelo e texturas do F-18 | `low-poly-f18-super-hornet.zip`, fornecido pelo usuário; glTF exportado pelo Blockbench |
| Cenário, interface, código e áudio procedural | Criados para SkyRacers |

O ZIP não inclui autor, URL ou licença; completar os créditos antes da publicação. Original preservado em `Assets/ThirdParty/F18/model.gltf`, com hierarquia preservada na conversão.

Referências técnicas: [glTF](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html), [NASA — Lift to Drag Ratio](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/lift-to-drag-ratio/), [NASA — Modern Drag Equation](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/modern-drag-equation/), [NASA — F-18 HARV](https://www.nasa.gov/reference/f-18-harv/), [VAZGRIZ](https://vazgriz.com/346/flight-simulator-in-unity3d-part-1/), [Aircraft Physics](https://github.com/gasgiant/Aircraft-Physics) e [Unity — AddTorque](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/rigidbody/addtorque). Implementação própria; nenhum pacote externo de física foi importado.

## Versionamento

Versionar `Assets/` com `.meta`, `Packages/` e `ProjectSettings/`. O `.gitignore` exclui caches, configurações locais, arquivos das IDEs e builds. Recordes da missão de retorno usam chave própria, separada dos percursos anteriores.


## Refinamento de contramedidas e cenário — 24/11/2026

Cada carga dispara 24 flares em leque bilateral, com brilho branco/dourado, rastro quente curto e fumaça persistente. As emissões acompanham a posição atual do caça durante a salva; pausa congela a sequência e reinício a elimina. O estoque continua em dez acionamentos.

SAMs partem a 300 m/s, aceleram a 240 m/s² e atingem 950 m/s. O limite de giro por carga G foi mantido: maior velocidade também exige maior raio de curva. No ensaio retilíneo, um míssil 2 km atrás de um alvo a 450 m/s o alcança em menos de sete segundos; curvas, relevo e flares alteram esse resultado em jogo.

Terreno externo ampliado para aproximadamente 48 km de largura, com montanhas e 16 km adicionais em cada extremidade do terreno anterior. As novas malhas compartilham as bordas do cânion para fechar as laterais e cabeceiras, sem alargar o corredor da missão. O mapa continua finito; suas bordas ficam afastadas do percurso e além da distância de renderização durante o voo pela missão.

Conferir em Play: salva vista pela câmera traseira e orbital, estoque por acionamento, pausa/reinício durante a salva, perseguição dos SAMs e ausência de bordas visíveis sobre o vale.


## Passagem pela ponte e acabamento da câmera

Corrigida a validação da ponte: usa a abertura real, sem o recuo fixo de 8 m por lado que causava derrotas em passagens deslocadas do centro. As colisões das asas/fuselagem continuam sendo tratadas pelos colisores do avião e da ponte; desvios fora do arco continuam inválidos. Testes cobrem passagem lateral a ±3,5 m, parte alta do arco e rejeição fora do vão.

Câmera com pós-processamento URP ativado, vinheta de intensidade 0,14/suavidade 0,55 e aberração cromática 0,04. Perfil em execução sobrescreve apenas esses parâmetros, mantendo os assets de volume da cena. HUD permanece em overlay. Conferir visual e passagem em ambos os sentidos no Play Mode.


## Arcos de missão e radar em ambas as pernas

A missão agora possui dois arcos sinalizadores físicos, além da ponte central: arco de RETORNO no último setor e arco de BASE no primeiro setor. Cruzar o interior do arco final na ida inicia o retorno; cruzar o arco inicial na volta, após os setores obrigatórios, conclui a missão. Passar acima ou ao lado não ativa o marco; é possível reposicionar e tentar a passagem novamente.

Regra atual de radar (substitui a restrição à volta descrita anteriormente): SAMs podem adquirir e lançar tanto na ida quanto na volta, se o caça estiver acima do teto, dentro do alcance e visível à bateria. Voo livre permanece sem SAMs.


## Leitura e agilidade dos SAMs

HUD lateral lista cada míssil ativo com distância ao caça em metros e direção relativa (frente, atrás, esquerda ou direita). O alerta principal destaca a distância do mais próximo, mantendo o centro livre. Rastro de fumaça mais espesso e duradouro, com pluma maior e mais visível.

Guiagem atual: até 32 G e 100°/s, substituindo o envelope anterior de 10,5 G/75°/s. A 950 m/s o giro passa de cerca de 6,2°/s para 18,9°/s. Velocidade, condições de disparo e flares permanecem iguais. Compilação e ensaios numéricos verificados; conferir legibilidade, efeitos e dificuldade no Play Mode.


## Ajuste de nuvens e lente

Vinheta elevada para 0,23 e aberração cromática para 0,12. Nuvens maiores e mais opacas, com distribuição próxima ao percurso e banco garantido à frente dos dois pontos de partida. O sistema avança uma etapa antes de pausar as partículas e define bounds explícitos para renderização estática e troca de modos. Conferir no Play Mode a aparência no céu e nas bordas da imagem.
