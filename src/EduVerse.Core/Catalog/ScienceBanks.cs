namespace EduVerse.Core.Catalog;

/// <summary>Physics: formulas computed in code (g = 10 m/s² unless stated) and checked facts.</summary>
internal static class PhysicsBank
{
    public static QuizQuestion Generate(Mc q) => q.Level switch
    {
        <= Level.Primary3 => q.Topic(f => Facts.Ask(f, FactList), Speed)(q),
        Level.Secondary1 => q.Topic(f => Facts.Ask(f, FactList), Speed, Density, Weight)(q),
        Level.Secondary2 => q.Topic(Ohm, Pressure, Acceleration, KineticEnergy)(q),
        Level.HighSchool => q.Topic(Momentum, PotentialEnergy, Waves, FreeFall)(q),
        Level.Bachelor => q.Topic(Resistors, Centripetal, Range)(q),
        Level.Master => q.Topic(f => Facts.Ask(f, FactList), Lorentz)(q),
        _ => q.Topic(Lorentz, f => Facts.Ask(f, FactList))(q),
    };

    private static readonly Fact[] FactList =
    [
        Fact.Of(Level.Primary3, "physics.states", "temperature", "At what temperature does pure water boil at sea level?|¿A qué temperatura hierve el agua pura al nivel del mar?|À quelle température l'eau pure bout-elle au niveau de la mer ?|A quina temperatura bull l'aigua pura al nivell del mar?", "100 °C", null, "♨️"),
        Fact.Of(Level.Primary3, "physics.states", "temperature", "At what temperature does pure water freeze?|¿A qué temperatura se congela el agua pura?|À quelle température l'eau pure gèle-t-elle ?|A quina temperatura es congela l'aigua pura?", "0 °C", null, "🧊"),
        Fact.Of(Level.Primary3, "physics.states", "temperature", "What is the normal temperature of the human body?|¿Cuál es la temperatura normal del cuerpo humano?|Quelle est la température normale du corps humain ?|Quina és la temperatura normal del cos humà?", "37 °C"),
        Fact.Of(Level.Primary3, "physics.states", "temperature", "What is absolute zero in degrees Celsius?|¿Cuánto es el cero absoluto en grados Celsius?|Combien vaut le zéro absolu en degrés Celsius ?|Quant és el zero absolut en graus Celsius?", "−273 °C"),
        Fact.Of(Level.Primary3, "physics.units", "unit", "Unit of force?|¿Unidad de fuerza?|Unité de force ?|Unitat de força?", "newton (N)"),
        Fact.Of(Level.Primary3, "physics.units", "unit", "Unit of energy?|¿Unidad de energía?|Unité d'énergie ?|Unitat d'energia?", "joule (J)"),
        Fact.Of(Level.Secondary1, "physics.units", "unit", "Unit of power?|¿Unidad de potencia?|Unité de puissance ?|Unitat de potència?", "watt (W)"),
        Fact.Of(Level.Secondary1, "physics.units", "unit", "Unit of pressure?|¿Unidad de presión?|Unité de pression ?|Unitat de pressió?", "pascal (Pa)"),
        Fact.Of(Level.Secondary1, "physics.units", "unit", "Unit of electrical resistance?|¿Unidad de resistencia eléctrica?|Unité de résistance électrique ?|Unitat de resistència elèctrica?", "ohm (Ω)"),
        Fact.Of(Level.Secondary1, "physics.units", "unit", "Unit of frequency?|¿Unidad de frecuencia?|Unité de fréquence ?|Unitat de freqüència?", "hertz (Hz)"),
        Fact.Of(Level.Master, "physics.modern", "law", "Which theorem links every continuous symmetry to a conserved quantity?|¿Qué teorema asocia cada simetría continua a una magnitud conservada?|Quel théorème associe chaque symétrie continue à une quantité conservée ?|Quin teorema associa cada simetria contínua a una magnitud conservada?", "Noether's theorem|teorema de Noether|théorème de Noether|teorema de Noether"),
        Fact.Of(Level.Master, "physics.modern", "law", "Which principle forbids two identical fermions from sharing the same quantum state?|¿Qué principio impide que dos fermiones idénticos compartan el mismo estado cuántico?|Quel principe interdit à deux fermions identiques d'occuper le même état quantique ?|Quin principi impedeix que dos fermions idèntics comparteixin el mateix estat quàntic?", "Pauli exclusion principle|principio de exclusión de Pauli|principe d'exclusion de Pauli|principi d'exclusió de Pauli"),
        Fact.Of(Level.Master, "physics.modern", "law", "Which relation states Δx · Δp ≥ ħ/2?|¿Qué relación establece Δx · Δp ≥ ħ/2?|Quelle relation énonce Δx · Δp ≥ ħ/2 ?|Quina relació estableix Δx · Δp ≥ ħ/2?", "Heisenberg uncertainty principle|principio de incertidumbre de Heisenberg|principe d'incertitude de Heisenberg|principi d'incertesa de Heisenberg"),
        Fact.Of(Level.Master, "physics.modern", "law", "Which equations unify electricity, magnetism and light?|¿Qué ecuaciones unifican electricidad, magnetismo y luz?|Quelles équations unifient électricité, magnétisme et lumière ?|Quines equacions unifiquen electricitat, magnetisme i llum?", "Maxwell's equations|ecuaciones de Maxwell|équations de Maxwell|equacions de Maxwell"),
        Fact.Of(Level.Doctorate, "physics.modern", "law", "Which equation gives the time evolution of a non-relativistic quantum state?|¿Qué ecuación da la evolución temporal de un estado cuántico no relativista?|Quelle équation donne l'évolution temporelle d'un état quantique non relativiste ?|Quina equació dona l'evolució temporal d'un estat quàntic no relativista?", "Schrödinger equation|ecuación de Schrödinger|équation de Schrödinger|equació de Schrödinger"),
        Fact.Of(Level.Doctorate, "physics.modern", "law", "Which equation describes relativistic spin-½ particles and predicted antimatter?|¿Qué ecuación describe partículas relativistas de espín ½ y predijo la antimateria?|Quelle équation décrit les particules relativistes de spin ½ et a prédit l'antimatière ?|Quina equació descriu partícules relativistes d'espín ½ i va predir l'antimatèria?", "Dirac equation|ecuación de Dirac|équation de Dirac|equació de Dirac"),
        Fact.Of(Level.Doctorate, "physics.modern", "law", "Which field equations relate the curvature of spacetime to energy and momentum?|¿Qué ecuaciones de campo relacionan la curvatura del espacio-tiempo con la energía y el momento?|Quelles équations de champ relient la courbure de l'espace-temps à l'énergie et à l'impulsion ?|Quines equacions de camp relacionen la curvatura de l'espaitemps amb l'energia i el moment?", "Einstein field equations|ecuaciones de campo de Einstein|équations d'Einstein|equacions de camp d'Einstein"),
    ];

    private static QuizQuestion Speed(Mc q)
    {
        var v = q.Between(2, q.Difficulty == 1 ? 10 : 30);
        var t = q.Between(2, 12);
        return q.Number("physics.motion", q.F("A cyclist travels {0} km in {1} hours at a constant speed. What is the speed?|Un ciclista recorre {0} km en {1} horas a velocidad constante. ¿Cuál es su velocidad?|Un cycliste parcourt {0} km en {1} heures à vitesse constante. Quelle est sa vitesse ?|Un ciclista recorre {0} km en {1} hores a velocitat constant. Quina és la seva velocitat?", v * t, t),
            v, [v * t * t, v + t, v * t], q.F("speed = distance ÷ time = {0} ÷ {1} = {2} km/h.|velocidad = distancia ÷ tiempo = {0} ÷ {1} = {2} km/h.|vitesse = distance ÷ temps = {0} ÷ {1} = {2} km/h.|velocitat = distància ÷ temps = {0} ÷ {1} = {2} km/h.", v * t, t, v), unit: " km/h");
    }

    private static QuizQuestion Density(Mc q)
    {
        var d = q.Between(1, 12) * 0.5;
        var vol = q.Between(2, 10);
        return q.Number("physics.density", q.F("An object of {0} g has a volume of {1} cm³. Its density?|Un objeto de {0} g tiene un volumen de {1} cm³. ¿Su densidad?|Un objet de {0} g a un volume de {1} cm³. Sa masse volumique ?|Un objecte de {0} g té un volum de {1} cm³. La seva densitat?", q.N(d * vol), vol),
            d, [d * vol * vol, vol / d, d + 1], q.F("density = mass ÷ volume = {0} g/cm³.|densidad = masa ÷ volumen = {0} g/cm³.|masse volumique = masse ÷ volume = {0} g/cm³.|densitat = massa ÷ volum = {0} g/cm³.", q.N(d)), unit: " g/cm³");
    }

    private static QuizQuestion Weight(Mc q)
    {
        var m = q.Between(2, 90);
        return q.Number("physics.forces", q.F("Weight of a {0} kg mass on Earth (g = 10 m/s²)?|¿Peso de una masa de {0} kg en la Tierra (g = 10 m/s²)?|Poids d'une masse de {0} kg sur Terre (g = 10 m/s²) ?|Pes d'una massa de {0} kg a la Terra (g = 10 m/s²)?", m),
            10 * m, [m, m / 10.0, m + 10], q.F("P = m · g = {0} × 10 = {1} N.|P = m · g = {0} × 10 = {1} N.|P = m · g = {0} × 10 = {1} N.|P = m · g = {0} × 10 = {1} N.", m, 10 * m), unit: " N");
    }

    private static QuizQuestion Ohm(Mc q)
    {
        var i = q.Between(1, 10) * 0.5;
        var r = q.Between(2, 50);
        return q.Number("physics.electricity", q.F("A current of {0} A flows through a {1} Ω resistor. Voltage?|Por una resistencia de {1} Ω circula una corriente de {0} A. ¿Tensión?|Un courant de {0} A traverse une résistance de {1} Ω. Tension ?|Per una resistència de {1} Ω circula un corrent de {0} A. Tensió?", q.N(i), r),
            i * r, [r / i, i + r, i * r * 2], q.F("Ohm's law: V = R · I = {0} × {1} = {2} V.|Ley de Ohm: V = R · I = {0} × {1} = {2} V.|Loi d'Ohm : U = R · I = {0} × {1} = {2} V.|Llei d'Ohm: V = R · I = {0} × {1} = {2} V.", r, q.N(i), q.N(i * r)), visual: Diagram.Circuit(i, r), unit: " V");
    }

    private static QuizQuestion Pressure(Mc q)
    {
        var p = q.Between(2, 40) * 50;
        var area = q.Between(1, 8) * 0.5;
        return q.Number("physics.forces", q.F("A force of {0} N acts on {1} m². Pressure?|Una fuerza de {0} N actúa sobre {1} m². ¿Presión?|Une force de {0} N s'exerce sur {1} m². Pression ?|Una força de {0} N actua sobre {1} m². Pressió?", q.N(p * area), q.N(area)),
            p, [p * area * area, area / (p * area), p * 2], q.F("p = F ÷ A = {0} Pa.|p = F ÷ A = {0} Pa.|p = F ÷ S = {0} Pa.|p = F ÷ A = {0} Pa.", p), unit: " Pa", decimals: 4);
    }

    private static QuizQuestion Acceleration(Mc q)
    {
        var a = q.Between(1, 9);
        var t = q.Between(2, 10);
        return q.Number("physics.motion", q.F("A car goes from 0 to {0} m/s in {1} s. Its acceleration?|Un coche pasa de 0 a {0} m/s en {1} s. ¿Su aceleración?|Une voiture passe de 0 à {0} m/s en {1} s. Son accélération ?|Un cotxe passa de 0 a {0} m/s en {1} s. La seva acceleració?", a * t, t),
            a, [a * t * t, a + t, a * t], q.F("a = Δv ÷ t = {0} ÷ {1} = {2} m/s².|a = Δv ÷ t = {0} ÷ {1} = {2} m/s².|a = Δv ÷ t = {0} ÷ {1} = {2} m/s².|a = Δv ÷ t = {0} ÷ {1} = {2} m/s².", a * t, t, a), unit: " m/s²");
    }

    private static QuizQuestion KineticEnergy(Mc q)
    {
        var m = q.Between(1, 10) * 2;
        var v = q.Between(2, 12);
        var e = m * v * v / 2;
        return q.Number("physics.energy", q.F("Kinetic energy of a {0} kg body moving at {1} m/s?|¿Energía cinética de un cuerpo de {0} kg a {1} m/s?|Énergie cinétique d'un corps de {0} kg à {1} m/s ?|Energia cinètica d'un cos de {0} kg a {1} m/s?", m, v),
            e, [m * v * v, m * v / 2, m * v], q.F("Ec = ½ m v² = ½ × {0} × {1}² = {2} J.|Ec = ½ m v² = ½ × {0} × {1}² = {2} J.|Ec = ½ m v² = ½ × {0} × {1}² = {2} J.|Ec = ½ m v² = ½ × {0} × {1}² = {2} J.", m, v, e), unit: " J");
    }

    private static QuizQuestion Momentum(Mc q)
    {
        var m = q.Between(1, 20);
        var v = q.Between(2, 25);
        return q.Number("physics.dynamics", q.F("Momentum of a {0} kg object at {1} m/s?|¿Cantidad de movimiento de un objeto de {0} kg a {1} m/s?|Quantité de mouvement d'un objet de {0} kg à {1} m/s ?|Quantitat de moviment d'un objecte de {0} kg a {1} m/s?", m, v),
            m * v, [m * v * v / 2.0, m + v, v / (double)m], q.F("p = m · v = {0} kg·m/s.|p = m · v = {0} kg·m/s.|p = m · v = {0} kg·m/s.|p = m · v = {0} kg·m/s.", m * v), unit: " kg·m/s");
    }

    private static QuizQuestion PotentialEnergy(Mc q)
    {
        var m = q.Between(1, 50);
        var h = q.Between(2, 30);
        return q.Number("physics.energy", q.F("Gravitational potential energy of {0} kg at {1} m (g = 10 m/s²)?|¿Energía potencial gravitatoria de {0} kg a {1} m (g = 10 m/s²)?|Énergie potentielle de pesanteur de {0} kg à {1} m (g = 10 m/s²) ?|Energia potencial gravitatòria de {0} kg a {1} m (g = 10 m/s²)?", m, h),
            10 * m * h, [m * h, 10 * m + h, m * h / 10.0], q.F("Ep = m · g · h = {0} × 10 × {1} = {2} J.|Ep = m · g · h = {0} × 10 × {1} = {2} J.|Ep = m · g · h = {0} × 10 × {1} = {2} J.|Ep = m · g · h = {0} × 10 × {1} = {2} J.", m, h, 10 * m * h), unit: " J");
    }

    private static QuizQuestion Waves(Mc q)
    {
        var f = q.Between(2, 50) * 10;
        var l = q.Between(1, 8) * 0.5;
        return q.Number("physics.waves", q.F("A wave has a frequency of {0} Hz and a wavelength of {1} m. Its speed?|Una onda tiene una frecuencia de {0} Hz y una longitud de onda de {1} m. ¿Su velocidad?|Une onde a une fréquence de {0} Hz et une longueur d'onde de {1} m. Sa vitesse ?|Una ona té una freqüència de {0} Hz i una longitud d'ona de {1} m. La seva velocitat?", f, q.N(l)),
            f * l, [f / l, f + l, l / f], q.F("v = f · λ = {0} m/s.|v = f · λ = {0} m/s.|v = f · λ = {0} m/s.|v = f · λ = {0} m/s.", q.N(f * l)), unit: " m/s", decimals: 4);
    }

    private static QuizQuestion FreeFall(Mc q)
    {
        var t = q.Between(1, 6);
        return q.Number("physics.motion", q.F("An object is dropped from rest. Distance fallen after {0} s (g = 10 m/s², no air)?|Se deja caer un objeto desde el reposo. ¿Distancia recorrida tras {0} s (g = 10 m/s², sin aire)?|On lâche un objet sans vitesse initiale. Distance parcourue après {0} s (g = 10 m/s², sans air) ?|Es deixa caure un objecte des del repòs. Distància recorreguda després de {0} s (g = 10 m/s², sense aire)?", t),
            5 * t * t, [10 * t * t, 10 * t, 5 * t], q.F("d = ½ g t² = 5 × {0}² = {1} m.|d = ½ g t² = 5 × {0}² = {1} m.|d = ½ g t² = 5 × {0}² = {1} m.|d = ½ g t² = 5 × {0}² = {1} m.", t, 5 * t * t), unit: " m");
    }

    private static QuizQuestion Resistors(Mc q)
    {
        var n = q.Between(2, 5);
        var r = q.Between(1, 12) * 10;
        var parallel = q.Rng.Next(2) == 0;
        var answer = parallel ? r / (double)n : r * n;
        return q.Number("physics.electricity", q.F(parallel ? "{0} identical {1} Ω resistors in parallel. Equivalent resistance?|{0} resistencias iguales de {1} Ω en paralelo. ¿Resistencia equivalente?|{0} résistances identiques de {1} Ω en parallèle. Résistance équivalente ?|{0} resistències iguals de {1} Ω en paral·lel. Resistència equivalent?" : "{0} identical {1} Ω resistors in series. Equivalent resistance?|{0} resistencias iguales de {1} Ω en serie. ¿Resistencia equivalente?|{0} résistances identiques de {1} Ω en série. Résistance équivalente ?|{0} resistències iguals de {1} Ω en sèrie. Resistència equivalent?", n, r),
            answer, [parallel ? r * n : r / (double)n, r, r + n], q.T("Series: resistances add up. Parallel: the inverses add up (n equal resistors → R/n).|En serie se suman las resistencias. En paralelo se suman las inversas (n iguales → R/n).|En série, les résistances s'additionnent. En parallèle, ce sont les inverses (n identiques → R/n).|En sèrie se sumen les resistències. En paral·lel se sumen les inverses (n iguals → R/n)."), visual: Diagram.Resistors(n, r, parallel), unit: " Ω");
    }

    private static QuizQuestion Centripetal(Mc q)
    {
        var v = q.Between(2, 20);
        var r = q.Pick([1, 2, 4, 5, 10]);
        return q.Number("physics.dynamics", q.F("Centripetal acceleration at {0} m/s on a circle of radius {1} m?|¿Aceleración centrípeta a {0} m/s en una circunferencia de radio {1} m?|Accélération centripète à {0} m/s sur un cercle de rayon {1} m ?|Acceleració centrípeta a {0} m/s en una circumferència de radi {1} m?", v, r),
            v * v / (double)r, [v / (double)r, v * v * (double)r, v * r], q.F("a = v² / r = {0} / {1} = {2} m/s².|a = v² / r = {0} / {1} = {2} m/s².|a = v² / r = {0} / {1} = {2} m/s².|a = v² / r = {0} / {1} = {2} m/s².", v * v, r, q.N(v * v / (double)r)), unit: " m/s²");
    }

    private static QuizQuestion Range(Mc q)
    {
        var v = q.Between(2, 10) * 5;
        return q.Number("physics.motion", q.F("A projectile is launched at {0} m/s and 45° on flat ground. Range (g = 10 m/s², no air)?|Un proyectil sale a {0} m/s con 45° sobre suelo llano. ¿Alcance (g = 10 m/s², sin aire)?|Un projectile est lancé à {0} m/s sous 45° sur un sol plat. Portée (g = 10 m/s², sans air) ?|Un projectil surt a {0} m/s amb 45° sobre terra pla. Abast (g = 10 m/s², sense aire)?", v),
            v * v / 10.0, [v * v / 20.0, v * v / 5.0, v], q.F("R = v² · sin(2θ) / g = {0}² × 1 / 10 = {1} m.|R = v² · sen(2θ) / g = {0}² × 1 / 10 = {1} m.|R = v² · sin(2θ) / g = {0}² × 1 / 10 = {1} m.|R = v² · sin(2θ) / g = {0}² × 1 / 10 = {1} m.", v, q.N(v * v / 10.0)), unit: " m");
    }

    private static QuizQuestion Lorentz(Mc q)
    {
        var (beta, gamma) = q.Pick([(0.6, 1.25), (0.8, 5 / 3.0), (0.28, 25 / 24.0), (0.96, 25 / 7.0)]);
        return q.Number("physics.relativity", q.F("Lorentz factor γ for a speed of {0}c?|¿Factor de Lorentz γ para una velocidad de {0}c?|Facteur de Lorentz γ pour une vitesse de {0}c ?|Factor de Lorentz γ per a una velocitat de {0}c?", q.N(beta)),
            gamma, [1 / gamma, 1 + beta, Math.Sqrt(1 - beta * beta)], q.F("γ = 1 / √(1 − v²/c²) = 1 / √(1 − {0}²) ≈ {1}.|γ = 1 / √(1 − v²/c²) = 1 / √(1 − {0}²) ≈ {1}.|γ = 1 / √(1 − v²/c²) = 1 / √(1 − {0}²) ≈ {1}.|γ = 1 / √(1 − v²/c²) = 1 / √(1 − {0}²) ≈ {1}.", q.N(beta), q.N(gamma)));
    }
}

/// <summary>Chemistry: periodic table data, stoichiometry and equilibria computed in code, checked facts.</summary>
internal static class ChemistryBank
{
    private static readonly (string Name, string Symbol, int Z)[] Elements =
    [
        ("hydrogen|hidrógeno|hydrogène|hidrogen", "H", 1), ("helium|helio|hélium|heli", "He", 2), ("carbon|carbono|carbone|carboni", "C", 6),
        ("nitrogen|nitrógeno|azote|nitrogen", "N", 7), ("oxygen|oxígeno|oxygène|oxigen", "O", 8), ("sodium|sodio|sodium|sodi", "Na", 11),
        ("magnesium|magnesio|magnésium|magnesi", "Mg", 12), ("aluminium|aluminio|aluminium|alumini", "Al", 13), ("sulfur|azufre|soufre|sofre", "S", 16),
        ("chlorine|cloro|chlore|clor", "Cl", 17), ("potassium|potasio|potassium|potassi", "K", 19), ("calcium|calcio|calcium|calci", "Ca", 20),
        ("iron|hierro|fer|ferro", "Fe", 26), ("copper|cobre|cuivre|coure", "Cu", 29), ("zinc|cinc|zinc|zinc", "Zn", 30),
        ("silver|plata|argent|plata", "Ag", 47), ("gold|oro|or|or", "Au", 79), ("lead|plomo|plomb|plom", "Pb", 82),
    ];

    private static readonly (string Formula, double Mass)[] Molecules =
    [
        ("H₂O", 18), ("CO₂", 44), ("CH₄", 16), ("NH₃", 17), ("O₂", 32), ("NaCl", 58.5), ("H₂SO₄", 98), ("C₆H₁₂O₆", 180), ("HCl", 36.5),
    ];

    private static readonly Fact[] FactList =
    [
        Fact.Of(Level.Primary3, "chemistry.matter", "formula", "Chemical formula of water?|¿Fórmula química del agua?|Formule chimique de l'eau ?|Fórmula química de l'aigua?", "H₂O", null, "💧"),
        Fact.Of(Level.Primary3, "chemistry.matter", "formula", "Chemical formula of carbon dioxide?|¿Fórmula química del dióxido de carbono?|Formule chimique du dioxyde de carbone ?|Fórmula química del diòxid de carboni?", "CO₂"),
        Fact.Of(Level.Primary3, "chemistry.matter", "formula", "Chemical formula of table salt?|¿Fórmula química de la sal común?|Formule chimique du sel de table ?|Fórmula química de la sal comuna?", "NaCl", null, "🧂"),
        Fact.Of(Level.Primary3, "chemistry.matter", "formula", "Chemical formula of oxygen gas?|¿Fórmula química del oxígeno gaseoso?|Formule chimique du dioxygène ?|Fórmula química de l'oxigen gasós?", "O₂"),
        Fact.Of(Level.Primary3, "chemistry.matter", "formula", "Chemical formula of methane?|¿Fórmula química del metano?|Formule chimique du méthane ?|Fórmula química del metà?", "CH₄"),
        Fact.Of(Level.HighSchool, "chemistry.constants", "constant", "Avogadro's number (particles per mole)?|¿Número de Avogadro (partículas por mol)?|Nombre d'Avogadro (particules par mole) ?|Nombre d'Avogadro (partícules per mol)?", "6.022 × 10²³|6,022 × 10²³|6,022 × 10²³|6,022 × 10²³"),
        Fact.Of(Level.HighSchool, "chemistry.constants", "constant", "Molar volume of an ideal gas at 0 °C and 1 atm?|¿Volumen molar de un gas ideal a 0 °C y 1 atm?|Volume molaire d'un gaz parfait à 0 °C et 1 atm ?|Volum molar d'un gas ideal a 0 °C i 1 atm?", "22.4 L|22,4 L|22,4 L|22,4 L"),
        Fact.Of(Level.HighSchool, "chemistry.constants", "constant", "Ideal gas constant R?|¿Constante de los gases ideales R?|Constante des gaz parfaits R ?|Constant dels gasos ideals R?", "8.314 J/(mol·K)|8,314 J/(mol·K)|8,314 J/(mol·K)|8,314 J/(mol·K)"),
        Fact.Of(Level.HighSchool, "chemistry.constants", "constant", "Ionic product of water Kw at 25 °C?|¿Producto iónico del agua Kw a 25 °C?|Produit ionique de l'eau Ke à 25 °C ?|Producte iònic de l'aigua Kw a 25 °C?", "10⁻¹⁴"),
        Fact.Of(Level.Master, "chemistry.bonding", "hybrid", "Hybridisation of carbon in methane (CH₄)?|¿Hibridación del carbono en el metano (CH₄)?|Hybridation du carbone dans le méthane (CH₄) ?|Hibridació del carboni al metà (CH₄)?", "sp³"),
        Fact.Of(Level.Master, "chemistry.bonding", "hybrid", "Hybridisation of carbon in ethylene (C₂H₄)?|¿Hibridación del carbono en el etileno (C₂H₄)?|Hybridation du carbone dans l'éthylène (C₂H₄) ?|Hibridació del carboni a l'etilè (C₂H₄)?", "sp²"),
        Fact.Of(Level.Master, "chemistry.bonding", "hybrid", "Hybridisation of carbon in acetylene (C₂H₂)?|¿Hibridación del carbono en el acetileno (C₂H₂)?|Hybridation du carbone dans l'acétylène (C₂H₂) ?|Hibridació del carboni a l'acetilè (C₂H₂)?", "sp"),
        Fact.Of(Level.Master, "chemistry.bonding", "hybrid", "Hybridisation of sulfur in SF₆?|¿Hibridación del azufre en SF₆?|Hybridation du soufre dans SF₆ ?|Hibridació del sofre a SF₆?", "sp³d²"),
        Fact.Of(Level.Doctorate, "chemistry.theory", "theory", "Which theory explains reaction rates through an activated complex in quasi-equilibrium?|¿Qué teoría explica la velocidad de reacción con un complejo activado en cuasiequilibrio?|Quelle théorie explique la vitesse de réaction par un complexe activé en quasi-équilibre ?|Quina teoria explica la velocitat de reacció amb un complex activat en quasiequilibri?", "transition state theory|teoría del estado de transición|théorie de l'état de transition|teoria de l'estat de transició"),
        Fact.Of(Level.Doctorate, "chemistry.theory", "theory", "Which method computes electronic structure from the electron density instead of the wave function?|¿Qué método calcula la estructura electrónica a partir de la densidad electrónica en lugar de la función de onda?|Quelle méthode calcule la structure électronique à partir de la densité électronique plutôt que de la fonction d'onde ?|Quin mètode calcula l'estructura electrònica a partir de la densitat electrònica en lloc de la funció d'ona?", "density functional theory (DFT)|teoría del funcional de la densidad (DFT)|théorie de la fonctionnelle de la densité (DFT)|teoria del funcional de la densitat (DFT)"),
        Fact.Of(Level.Doctorate, "chemistry.theory", "theory", "Which approximation separates nuclear and electronic motion?|¿Qué aproximación separa el movimiento nuclear del electrónico?|Quelle approximation sépare les mouvements nucléaire et électronique ?|Quina aproximació separa el moviment nuclear de l'electrònic?", "Born–Oppenheimer approximation|aproximación de Born-Oppenheimer|approximation de Born-Oppenheimer|aproximació de Born-Oppenheimer"),
        Fact.Of(Level.Doctorate, "chemistry.theory", "theory", "Which rule predicts that thermal pericyclic reactions conserve orbital symmetry?|¿Qué reglas predicen que las reacciones pericíclicas térmicas conservan la simetría orbital?|Quelles règles prédisent que les réactions péricycliques thermiques conservent la symétrie orbitalaire ?|Quines regles prediuen que les reaccions pericícliques tèrmiques conserven la simetria orbital?", "Woodward–Hoffmann rules|reglas de Woodward-Hoffmann|règles de Woodward-Hoffmann|regles de Woodward-Hoffmann"),
    ];

    public static QuizQuestion Generate(Mc q) => q.Level switch
    {
        <= Level.Primary3 => q.Topic(f => Facts.Ask(f, FactList), Symbol)(q),
        Level.Secondary1 => q.Topic(Symbol, Protons, f => Facts.Ask(f, FactList))(q),
        Level.Secondary2 => q.Topic(Protons, Neutrons, MolarMass)(q),
        Level.HighSchool => q.Topic(Moles, PhAcid, Molarity, f => Facts.Ask(f, FactList))(q),
        Level.Bachelor => q.Topic(Dilution, PhBase, HalfLife)(q),
        Level.Master => q.Topic(f => Facts.Ask(f, FactList), Gibbs, RateOrder)(q),
        _ => q.Topic(Gibbs, RateOrder, f => Facts.Ask(f, FactList))(q),
    };

    private static QuizQuestion Symbol(Mc q)
    {
        var e = q.Pick(q.Difficulty == 1 ? Elements[..8] : Elements);
        return q.Choice("chemistry.elements", q.F("Chemical symbol of {0}?|¿Símbolo químico del {0}?|Symbole chimique du {0} ?|Símbol químic del {0}?", q.T(e.Name)), e.Symbol,
            Elements.Select(x => x.Symbol).Concat(["Ir", "Fr", "Hy", "Co"]), q.F("{0}: {1}. Some symbols come from Latin names (Fe ferrum, Au aurum).|{0}: {1}. Algunos símbolos vienen del latín (Fe ferrum, Au aurum).|{0} : {1}. Certains symboles viennent du latin (Fe ferrum, Au aurum).|{0}: {1}. Alguns símbols vénen del llatí (Fe ferrum, Au aurum).", q.T(e.Name), e.Symbol));
    }

    private static QuizQuestion Protons(Mc q)
    {
        var e = q.Pick(Elements);
        return q.Number("chemistry.atoms", q.F("How many protons does an atom of {0} ({1}) have?|¿Cuántos protones tiene un átomo de {0} ({1})?|Combien de protons possède un atome de {0} ({1}) ?|Quants protons té un àtom de {0} ({1})?", q.T(e.Name), e.Symbol),
            e.Z, [e.Z + 1, e.Z * 2, e.Z - 1], q.F("The atomic number Z of {0} is {1}: that is the number of protons.|El número atómico Z del {0} es {1}: es el número de protones.|Le numéro atomique Z du {0} est {1} : c'est le nombre de protons.|El nombre atòmic Z del {0} és {1}: és el nombre de protons.", q.T(e.Name), e.Z));
    }

    private static QuizQuestion Neutrons(Mc q)
    {
        var (isotope, z, a) = q.Pick([("¹²C", 6, 12), ("¹⁴C", 6, 14), ("¹⁶O", 8, 16), ("²³Na", 11, 23), ("⁵⁶Fe", 26, 56), ("³⁵Cl", 17, 35), ("²³⁸U", 92, 238), ("⁴⁰Ca", 20, 40)]);
        return q.Number("chemistry.atoms", q.F("Neutrons in {0} (Z = {1})?|¿Neutrones en {0} (Z = {1})?|Neutrons dans {0} (Z = {1}) ?|Neutrons a {0} (Z = {1})?", isotope, z),
            a - z, [a, z, a + z], q.F("N = A − Z = {0} − {1} = {2}.|N = A − Z = {0} − {1} = {2}.|N = A − Z = {0} − {1} = {2}.|N = A − Z = {0} − {1} = {2}.", a, z, a - z));
    }

    private static QuizQuestion MolarMass(Mc q)
    {
        var (formula, mass) = q.Pick(Molecules);
        return q.Number("chemistry.stoichiometry", q.F("Molar mass of {0} (H = 1, C = 12, N = 14, O = 16, Na = 23, S = 32, Cl = 35.5)?|¿Masa molar de {0} (H = 1, C = 12, N = 14, O = 16, Na = 23, S = 32, Cl = 35,5)?|Masse molaire de {0} (H = 1, C = 12, N = 14, O = 16, Na = 23, S = 32, Cl = 35,5) ?|Massa molar de {0} (H = 1, C = 12, N = 14, O = 16, Na = 23, S = 32, Cl = 35,5)?", formula),
            mass, [mass + 16, mass - 1, mass * 2], q.T("Add the atomic masses of every atom of the formula.|Suma las masas atómicas de todos los átomos de la fórmula.|Additionne les masses atomiques de tous les atomes de la formule.|Suma les masses atòmiques de tots els àtoms de la fórmula."), unit: " g/mol", decimals: 1);
    }

    private static QuizQuestion Moles(Mc q)
    {
        var (formula, mass) = q.Pick(Molecules.Where(m => m.Mass % 1 == 0).ToArray());
        var n = q.Between(1, 8) * 0.5;
        return q.Number("chemistry.stoichiometry", q.F("How many moles are there in {0} g of {1} (M = {2} g/mol)?|¿Cuántos moles hay en {0} g de {1} (M = {2} g/mol)?|Combien de moles y a-t-il dans {0} g de {1} (M = {2} g/mol) ?|Quants mols hi ha en {0} g de {1} (M = {2} g/mol)?", q.N(n * mass), formula, mass),
            n, [n * mass * mass, mass / (n * mass), n * 2], q.F("n = m / M = {0}.|n = m / M = {0}.|n = m / M = {0}.|n = m / M = {0}.", q.N(n)), unit: " mol", decimals: 4);
    }

    private static QuizQuestion PhAcid(Mc q)
    {
        var n = q.Between(1, 5);
        return q.Number("chemistry.acids", q.F("pH of a 10⁻{0} mol/L solution of HCl (strong acid)?|¿pH de una disolución 10⁻{0} mol/L de HCl (ácido fuerte)?|pH d'une solution de HCl (acide fort) à 10⁻{0} mol/L ?|pH d'una dissolució 10⁻{0} mol/L d'HCl (àcid fort)?", Sup(n)),
            n, [14 - n, -n, n + 1], q.F("A strong acid dissociates completely: [H₃O⁺] = 10⁻{0}, pH = −log[H₃O⁺] = {1}.|Un ácido fuerte se disocia por completo: [H₃O⁺] = 10⁻{0}, pH = −log[H₃O⁺] = {1}.|Un acide fort se dissocie totalement : [H₃O⁺] = 10⁻{0}, pH = −log[H₃O⁺] = {1}.|Un àcid fort es dissocia completament: [H₃O⁺] = 10⁻{0}, pH = −log[H₃O⁺] = {1}.", Sup(n), n));
    }

    private static QuizQuestion PhBase(Mc q)
    {
        var n = q.Between(1, 5);
        return q.Number("chemistry.acids", q.F("pH of a 10⁻{0} mol/L solution of NaOH (strong base) at 25 °C?|¿pH de una disolución 10⁻{0} mol/L de NaOH (base fuerte) a 25 °C?|pH d'une solution de NaOH (base forte) à 10⁻{0} mol/L à 25 °C ?|pH d'una dissolució 10⁻{0} mol/L de NaOH (base forta) a 25 °C?", Sup(n)),
            14 - n, [n, 7 + n, 14 + n], q.F("pOH = {0}, so pH = 14 − {0} = {1}.|pOH = {0}, así que pH = 14 − {0} = {1}.|pOH = {0}, donc pH = 14 − {0} = {1}.|pOH = {0}, per tant pH = 14 − {0} = {1}.", n, 14 - n));
    }

    private static string Sup(int n) => "⁰¹²³⁴⁵⁶⁷⁸⁹"[n].ToString();

    private static QuizQuestion Molarity(Mc q)
    {
        var c = q.Between(1, 8) * 0.25;
        var v = q.Pick([0.25, 0.5, 1, 2]);
        return q.Number("chemistry.solutions", q.F("{0} mol of solute dissolved in {1} L of solution. Concentration?|{0} mol de soluto disueltos en {1} L de disolución. ¿Concentración?|{0} mol de soluté dissous dans {1} L de solution. Concentration ?|{0} mol de solut dissolts en {1} L de dissolució. Concentració?", q.N(c * v, 4), q.N(v)),
            c, [c * v * v, v / (c * v), c * 2], q.F("c = n / V = {0} mol/L.|c = n / V = {0} mol/L.|c = n / V = {0} mol/L.|c = n / V = {0} mol/L.", q.N(c)), unit: " mol/L", decimals: 4);
    }

    private static QuizQuestion Dilution(Mc q)
    {
        var c1 = q.Pick([0.5, 1, 2, 4]);
        var v1 = q.Pick([10, 20, 25, 50]);
        var factor = q.Pick([2, 4, 5, 10]);
        return q.Number("chemistry.solutions", q.F("{0} mL of a {1} mol/L solution are diluted to {2} mL. New concentration?|Se diluyen {0} mL de una disolución {1} mol/L hasta {2} mL. ¿Nueva concentración?|On dilue {0} mL d'une solution à {1} mol/L jusqu'à {2} mL. Nouvelle concentration ?|Es dilueixen {0} mL d'una dissolució {1} mol/L fins a {2} mL. Nova concentració?", v1, q.N(c1), v1 * factor),
            c1 / factor, [c1 * factor, c1 / (factor + 1.0), c1], q.F("c₁V₁ = c₂V₂, so c₂ = {0} × {1} / {2}.|c₁V₁ = c₂V₂, así que c₂ = {0} × {1} / {2}.|c₁V₁ = c₂V₂, donc c₂ = {0} × {1} / {2}.|c₁V₁ = c₂V₂, per tant c₂ = {0} × {1} / {2}.", q.N(c1), v1, v1 * factor), unit: " mol/L", decimals: 4);
    }

    private static QuizQuestion HalfLife(Mc q)
    {
        var n = q.Between(1, 5);
        var t = q.Pick([2, 5, 8, 10]);
        return q.Choice("chemistry.kinetics", q.F("A first-order reaction has a half-life of {0} min. What fraction remains after {1} min?|Una reacción de primer orden tiene una semivida de {0} min. ¿Qué fracción queda tras {1} min?|Une réaction d'ordre 1 a une demi-vie de {0} min. Quelle fraction reste-t-il après {1} min ?|Una reacció d'ordre 1 té una semivida de {0} min. Quina fracció queda després de {1} min?", t, t * n),
            Mc.Frac(1, 1L << n), [Mc.Frac(1, 2L * n), Mc.Frac(1, n + 1L), Mc.Frac(1, 1L << (n + 1)), Mc.Frac(n, 1L << (n + 1)), Mc.Frac(1, 3L * n), Mc.Frac(1, 1L << (n + 2)), "3/4"],
            q.F("{0} half-lives: (1/2)^{0} = {1}.|{0} semividas: (1/2)^{0} = {1}.|{0} demi-vies : (1/2)^{0} = {1}.|{0} semivides: (1/2)^{0} = {1}.", n, Mc.Frac(1, 1L << n)));
    }

    private static QuizQuestion Gibbs(Mc q)
    {
        var dh = q.Between(-12, 12) * 10;
        var ds = q.Between(-10, 10) * 20;
        var t = q.Pick([250, 300, 400, 500]);
        var dg = dh - t * ds / 1000.0;
        return q.Number("chemistry.thermodynamics", q.F("ΔH = {0} kJ/mol, ΔS = {1} J/(mol·K), T = {2} K. ΔG?|ΔH = {0} kJ/mol, ΔS = {1} J/(mol·K), T = {2} K. ¿ΔG?|ΔH = {0} kJ/mol, ΔS = {1} J/(mol·K), T = {2} K. ΔG ?|ΔH = {0} kJ/mol, ΔS = {1} J/(mol·K), T = {2} K. ΔG?", dh, ds, t),
            dg, [dh - t * ds, dh + t * ds / 1000.0, -dg], q.F("ΔG = ΔH − TΔS = {0} − {1} × ({2} / 1000) = {3} kJ/mol (negative: spontaneous).|ΔG = ΔH − TΔS = {0} − {1} × ({2} / 1000) = {3} kJ/mol (negativo: espontánea).|ΔG = ΔH − TΔS = {0} − {1} × ({2} / 1000) = {3} kJ/mol (négatif : spontanée).|ΔG = ΔH − TΔS = {0} − {1} × ({2} / 1000) = {3} kJ/mol (negatiu: espontània).", dh, t, ds, q.N(dg)), unit: " kJ/mol");
    }

    private static QuizQuestion RateOrder(Mc q)
    {
        var order = q.Between(0, 3);
        var k = q.Pick([2, 3]);
        var factor = (int)Math.Pow(k, order);
        return q.Number("chemistry.kinetics", q.F("When [A] is multiplied by {0}, the initial rate is multiplied by {1}. Order with respect to A?|Al multiplicar [A] por {0}, la velocidad inicial se multiplica por {1}. ¿Orden respecto de A?|Quand [A] est multipliée par {0}, la vitesse initiale est multipliée par {1}. Ordre par rapport à A ?|En multiplicar [A] per {0}, la velocitat inicial es multiplica per {1}. Ordre respecte d'A?", k, factor),
            order, [factor, k, order + 1, order + 2], q.F("rate ∝ [A]ⁿ: {0}ⁿ = {1}, so n = {2}.|v ∝ [A]ⁿ: {0}ⁿ = {1}, así que n = {2}.|v ∝ [A]ⁿ : {0}ⁿ = {1}, donc n = {2}.|v ∝ [A]ⁿ: {0}ⁿ = {1}, per tant n = {2}.", k, factor, order));
    }
}

/// <summary>Biology: checked facts, Punnett squares, base pairing and population genetics computed in code.</summary>
internal static class BiologyBank
{
    private static readonly Fact[] FactList =
    [
        Fact.Of(Level.Primary1, "biology.animals", "diet", "What does a cow eat?|¿Qué come una vaca?|Que mange une vache ?|Què menja una vaca?", "plants (herbivore)|plantas (herbívoro)|des plantes (herbivore)|plantes (herbívor)", null, "🐄"),
        Fact.Of(Level.Primary1, "biology.animals", "diet", "What does a lion eat?|¿Qué come un león?|Que mange un lion ?|Què menja un lleó?", "meat (carnivore)|carne (carnívoro)|de la viande (carnivore)|carn (carnívor)", null, "🦁"),
        Fact.Of(Level.Primary1, "biology.animals", "diet", "What does a bear eat?|¿Qué come un oso?|Que mange un ours ?|Què menja un ós?", "plants and meat (omnivore)|plantas y carne (omnívoro)|des plantes et de la viande (omnivore)|plantes i carn (omnívor)", null, "🐻"),
        Fact.Of(Level.Primary1, "biology.animals", "diet", "What does a rabbit eat?|¿Qué come un conejo?|Que mange un lapin ?|Què menja un conill?", "plants (herbivore)|plantas (herbívoro)|des plantes (herbivore)|plantes (herbívor)", null, "🐰"),
        Fact.Of(Level.Primary1, "biology.animals", "diet", "What does a hedgehog mostly eat?|¿Qué come sobre todo un erizo?|Que mange surtout un hérisson ?|Què menja sobretot un eriçó?", "insects (insectivore)|insectos (insectívoro)|des insectes (insectivore)|insectes (insectívor)", null, "🦔"),
        Fact.Of(Level.Primary2, "biology.vertebrates", "class", "What kind of animal is a dolphin?|¿Qué tipo de animal es un delfín?|Quel type d'animal est un dauphin ?|Quin tipus d'animal és un dofí?", "mammal|mamífero|mammifère|mamífer", null, "🐬"),
        Fact.Of(Level.Primary2, "biology.vertebrates", "class", "What kind of animal is a penguin?|¿Qué tipo de animal es un pingüino?|Quel type d'animal est un manchot ?|Quin tipus d'animal és un pingüí?", "bird|ave|oiseau|ocell", null, "🐧"),
        Fact.Of(Level.Primary2, "biology.vertebrates", "class", "What kind of animal is a frog?|¿Qué tipo de animal es una rana?|Quel type d'animal est une grenouille ?|Quin tipus d'animal és una granota?", "amphibian|anfibio|amphibien|amfibi", null, "🐸"),
        Fact.Of(Level.Primary2, "biology.vertebrates", "class", "What kind of animal is a turtle?|¿Qué tipo de animal es una tortuga?|Quel type d'animal est une tortue ?|Quin tipus d'animal és una tortuga?", "reptile|reptil|reptile|rèptil", null, "🐢"),
        Fact.Of(Level.Primary2, "biology.vertebrates", "class", "What kind of animal is a salmon?|¿Qué tipo de animal es un salmón?|Quel type d'animal est un saumon ?|Quin tipus d'animal és un salmó?", "fish|pez|poisson|peix", null, "🐟"),
        Fact.Of(Level.Primary2, "biology.vertebrates", "class", "What kind of animal is a bat?|¿Qué tipo de animal es un murciélago?|Quel type d'animal est une chauve-souris ?|Quin tipus d'animal és un ratpenat?", "mammal|mamífero|mammifère|mamífer", "It flies, but it feeds its young with milk: it is a mammal.|Vuela, pero alimenta a sus crías con leche: es un mamífero.|Elle vole, mais nourrit ses petits avec du lait : c'est un mammifère.|Vola, però alimenta les cries amb llet: és un mamífer.", "🦇"),
        Fact.Of(Level.Primary3, "biology.body", "organ", "Which organ pumps blood around the body?|¿Qué órgano bombea la sangre por el cuerpo?|Quel organe pompe le sang dans le corps ?|Quin òrgan bombeja la sang pel cos?", "the heart|el corazón|le cœur|el cor", null, "❤️"),
        Fact.Of(Level.Primary3, "biology.body", "organ", "Which organs take oxygen from the air?|¿Qué órganos toman el oxígeno del aire?|Quels organes prennent l'oxygène de l'air ?|Quins òrgans agafen l'oxigen de l'aire?", "the lungs|los pulmones|les poumons|els pulmons", null, "🫁"),
        Fact.Of(Level.Primary3, "biology.body", "organ", "Which organs filter the blood and make urine?|¿Qué órganos filtran la sangre y producen la orina?|Quels organes filtrent le sang et produisent l'urine ?|Quins òrgans filtren la sang i produeixen l'orina?", "the kidneys|los riñones|les reins|els ronyons"),
        Fact.Of(Level.Primary3, "biology.body", "organ", "Which organ controls the whole body and thinks?|¿Qué órgano controla todo el cuerpo y piensa?|Quel organe contrôle tout le corps et pense ?|Quin òrgan controla tot el cos i pensa?", "the brain|el cerebro|le cerveau|el cervell", null, "🧠"),
        Fact.Of(Level.Primary3, "biology.plants", "gas", "Which gas do plants take in for photosynthesis?|¿Qué gas toman las plantas para la fotosíntesis?|Quel gaz les plantes absorbent-elles pour la photosynthèse ?|Quin gas agafen les plantes per a la fotosíntesi?", "carbon dioxide (CO₂)|dióxido de carbono (CO₂)|dioxyde de carbone (CO₂)|diòxid de carboni (CO₂)", null, "🌿"),
        Fact.Of(Level.Primary3, "biology.plants", "gas", "Which gas do plants release during photosynthesis?|¿Qué gas liberan las plantas en la fotosíntesis?|Quel gaz les plantes libèrent-elles pendant la photosynthèse ?|Quin gas alliberen les plantes en la fotosíntesi?", "oxygen (O₂)|oxígeno (O₂)|dioxygène (O₂)|oxigen (O₂)", null, "🌳"),
        Fact.Of(Level.Primary3, "biology.plants", "gas", "Which gas makes up most of the air?|¿Qué gas forma la mayor parte del aire?|Quel gaz compose la plus grande partie de l'air ?|Quin gas forma la major part de l'aire?", "nitrogen (N₂)|nitrógeno (N₂)|diazote (N₂)|nitrogen (N₂)"),
        Fact.Of(Level.Primary3, "biology.plants", "gas", "Which gas fills airships because it is lighter than air and does not burn?|¿Qué gas llena los dirigibles porque es más ligero que el aire y no arde?|Quel gaz remplit les dirigeables car il est plus léger que l'air et ne brûle pas ?|Quin gas omple els dirigibles perquè és més lleuger que l'aire i no crema?", "helium (He)|helio (He)|hélium (He)|heli (He)"),
        Fact.Of(Level.Secondary1, "biology.cells", "organelle", "Which organelle produces most of the cell's ATP?|¿Qué orgánulo produce la mayor parte del ATP de la célula?|Quel organite produit la majeure partie de l'ATP de la cellule ?|Quin orgànul produeix la major part de l'ATP de la cèl·lula?", "mitochondrion|mitocondria|mitochondrie|mitocondri"),
        Fact.Of(Level.Secondary1, "biology.cells", "organelle", "Where is the DNA of an animal cell mainly kept?|¿Dónde se guarda principalmente el ADN de una célula animal?|Où l'ADN d'une cellule animale est-il principalement conservé ?|On es guarda principalment l'ADN d'una cèl·lula animal?", "nucleus|núcleo|noyau|nucli"),
        Fact.Of(Level.Secondary1, "biology.cells", "organelle", "Which structures build proteins?|¿Qué estructuras fabrican las proteínas?|Quelles structures fabriquent les protéines ?|Quines estructures fabriquen les proteïnes?", "ribosomes|ribosomas|ribosomes|ribosomes"),
        Fact.Of(Level.Secondary1, "biology.cells", "organelle", "Which organelle carries out photosynthesis in plant cells?|¿Qué orgánulo realiza la fotosíntesis en las células vegetales?|Quel organite réalise la photosynthèse dans les cellules végétales ?|Quin orgànul fa la fotosíntesi a les cèl·lules vegetals?", "chloroplast|cloroplasto|chloroplaste|cloroplast"),
        Fact.Of(Level.Secondary1, "biology.cells", "organelle", "Which organelle modifies and packages proteins for export?|¿Qué orgánulo modifica y empaqueta las proteínas para exportarlas?|Quel organite modifie et emballe les protéines pour l'export ?|Quin orgànul modifica i empaqueta les proteïnes per exportar-les?", "Golgi apparatus|aparato de Golgi|appareil de Golgi|aparell de Golgi"),
        Fact.Of(Level.HighSchool, "biology.genetics", "count", "How many chromosomes are there in a human body cell?|¿Cuántos cromosomas hay en una célula somática humana?|Combien de chromosomes y a-t-il dans une cellule somatique humaine ?|Quants cromosomes hi ha en una cèl·lula somàtica humana?", "46"),
        Fact.Of(Level.HighSchool, "biology.genetics", "count", "How many chromosomes are there in a human gamete?|¿Cuántos cromosomas hay en un gameto humano?|Combien de chromosomes y a-t-il dans un gamète humain ?|Quants cromosomes hi ha en un gàmeta humà?", "23"),
        Fact.Of(Level.HighSchool, "biology.genetics", "count", "How many different codons are there?|¿Cuántos codones distintos hay?|Combien de codons différents existe-t-il ?|Quants codons diferents hi ha?", "64"),
        Fact.Of(Level.HighSchool, "biology.genetics", "count", "How many standard amino acids are used to build proteins?|¿Cuántos aminoácidos estándar forman las proteínas?|Combien d'acides aminés standard composent les protéines ?|Quants aminoàcids estàndard formen les proteïnes?", "20"),
        Fact.Of(Level.HighSchool, "biology.genetics", "count", "How many cells does meiosis produce from one cell?|¿Cuántas células produce la meiosis a partir de una?|Combien de cellules la méiose produit-elle à partir d'une seule ?|Quantes cèl·lules produeix la meiosi a partir d'una?", "4"),
        Fact.Of(Level.Bachelor, "biology.molecular", "technique", "Which technique amplifies a DNA sequence exponentially?|¿Qué técnica amplifica una secuencia de ADN de forma exponencial?|Quelle technique amplifie une séquence d'ADN de façon exponentielle ?|Quina tècnica amplifica una seqüència d'ADN de manera exponencial?", "PCR"),
        Fact.Of(Level.Bachelor, "biology.molecular", "technique", "Which technique detects a specific protein with antibodies after electrophoresis?|¿Qué técnica detecta una proteína concreta con anticuerpos tras una electroforesis?|Quelle technique détecte une protéine précise avec des anticorps après électrophorèse ?|Quina tècnica detecta una proteïna concreta amb anticossos després d'una electroforesi?", "western blot"),
        Fact.Of(Level.Bachelor, "biology.molecular", "technique", "Which technique detects a specific DNA sequence with a probe?|¿Qué técnica detecta una secuencia de ADN concreta con una sonda?|Quelle technique détecte une séquence d'ADN précise avec une sonde ?|Quina tècnica detecta una seqüència d'ADN concreta amb una sonda?", "Southern blot"),
        Fact.Of(Level.Bachelor, "biology.molecular", "technique", "Which technique detects a specific RNA?|¿Qué técnica detecta un ARN concreto?|Quelle technique détecte un ARN précis ?|Quina tècnica detecta un ARN concret?", "Northern blot"),
        Fact.Of(Level.Master, "biology.molecular", "technique", "Which system edits a genome by cutting DNA where a guide RNA matches?|¿Qué sistema edita un genoma cortando el ADN donde coincide un ARN guía?|Quel système édite un génome en coupant l'ADN là où un ARN guide s'apparie ?|Quin sistema edita un genoma tallant l'ADN on coincideix un ARN guia?", "CRISPR-Cas9"),
        Fact.Of(Level.Master, "biology.molecular", "technique", "Which method measures the expression of all genes of a sample by sequencing?|¿Qué método mide la expresión de todos los genes de una muestra mediante secuenciación?|Quelle méthode mesure l'expression de tous les gènes d'un échantillon par séquençage ?|Quin mètode mesura l'expressió de tots els gens d'una mostra per seqüenciació?", "RNA-seq"),
        Fact.Of(Level.Doctorate, "biology.molecular", "technique", "Which method maps where a protein binds the genome using immunoprecipitation and sequencing?|¿Qué método localiza dónde se une una proteína al genoma con inmunoprecipitación y secuenciación?|Quelle méthode cartographie les sites de liaison d'une protéine au génome par immunoprécipitation et séquençage ?|Quin mètode localitza on s'uneix una proteïna al genoma amb immunoprecipitació i seqüenciació?", "ChIP-seq"),
        Fact.Of(Level.Doctorate, "biology.molecular", "technique", "Which method resolves protein structures from frozen samples without crystals?|¿Qué método resuelve estructuras de proteínas a partir de muestras congeladas sin cristales?|Quelle méthode résout des structures de protéines à partir d'échantillons congelés, sans cristaux ?|Quin mètode resol estructures de proteïnes a partir de mostres congelades sense cristalls?", "cryo-electron microscopy|criomicroscopía electrónica|cryo-microscopie électronique|criomicroscòpia electrònica"),
    ];

    public static QuizQuestion Generate(Mc q) => q.Level switch
    {
        <= Level.Primary3 => Facts.Ask(q, FactList),
        Level.Secondary1 => q.Topic(f => Facts.Ask(f, FactList), Complement)(q),
        Level.Secondary2 => q.Topic(Punnett, Complement, f => Facts.Ask(f, FactList))(q),
        Level.HighSchool => q.Topic(f => Facts.Ask(f, FactList), Punnett, Complement)(q),
        Level.Bachelor => q.Topic(f => Facts.Ask(f, FactList), HardyWeinberg, Punnett)(q),
        _ => q.Topic(f => Facts.Ask(f, FactList), HardyWeinberg, Pcr)(q),
    };

    private static QuizQuestion Punnett(Mc q)
    {
        var (p1, p2) = q.Pick([("Aa", "Aa"), ("Aa", "aa"), ("AA", "aa"), ("Aa", "AA")]);
        var recessive = p1.Count(c => c == 'a') * p2.Count(c => c == 'a');
        var answer = Mc.Frac(recessive, 4);
        return q.Choice("biology.heredity", q.F("Cross {0} × {1} (A dominant). Probability of a child with the recessive trait (aa)?|Cruce {0} × {1} (A dominante). ¿Probabilidad de un descendiente con el carácter recesivo (aa)?|Croisement {0} × {1} (A dominant). Probabilité d'un descendant au caractère récessif (aa) ?|Encreuament {0} × {1} (A dominant). Probabilitat d'un descendent amb el caràcter recessiu (aa)?", p1, p2),
            answer, ["0", "1/4", "1/2", "3/4", "1"], q.F("Each parent gives one allele: {0} of the 4 boxes of the Punnett square are aa.|Cada progenitor aporta un alelo: {0} de las 4 casillas del cuadro de Punnett son aa.|Chaque parent donne un allèle : {0} des 4 cases de l'échiquier de Punnett sont aa.|Cada progenitor aporta un al·lel: {0} de les 4 caselles del quadre de Punnett són aa.", recessive), Diagram.Punnett(p1, p2));
    }

    private static QuizQuestion Complement(Mc q)
    {
        var strand = new string(Enumerable.Range(0, q.Difficulty == 1 ? 5 : 7).Select(_ => "ATCG"[q.Rng.Next(4)]).ToArray());
        var rna = q.Difficulty == 3;
        string Pair(char c) => c switch { 'A' => rna ? "U" : "T", 'T' => "A", 'C' => "G", _ => "C" };
        var answer = string.Concat(strand.Select(Pair));
        var reversed = new string(answer.Reverse().ToArray());
        var swapped = string.Concat(strand.Select(c => c switch { 'A' => "G", 'G' => "A", 'C' => "T", _ => "C" }));
        var other = rna ? answer.Replace('U', 'T') : answer.Replace('T', 'U');
        return q.Choice("biology.dna", q.F(rna ? "Which mRNA is transcribed from the DNA template strand {0}?|¿Qué ARNm se transcribe de la hebra molde de ADN {0}?|Quel ARNm est transcrit à partir du brin matrice d'ADN {0} ?|Quin ARNm es transcriu de la cadena motlle d'ADN {0}?" : "Complementary strand of the DNA {0}?|¿Hebra complementaria del ADN {0}?|Brin complémentaire de l'ADN {0} ?|Cadena complementària de l'ADN {0}?", strand),
            answer, [reversed, swapped, strand, other], q.T(rna ? "A pairs with U in RNA, T with A, C with G.|En el ARN la A se empareja con U, la T con A y la C con G.|Dans l'ARN, A s'apparie avec U, T avec A, C avec G.|A l'ARN, l'A s'aparella amb U, la T amb A i la C amb G." : "A pairs with T and C pairs with G.|La A se empareja con la T y la C con la G.|A s'apparie avec T et C avec G.|L'A s'aparella amb la T i la C amb la G."));
    }

    private static QuizQuestion HardyWeinberg(Mc q)
    {
        var qa = q.Pick([0.1, 0.2, 0.3, 0.4, 0.5]);
        var p = 1 - qa;
        return q.Number("biology.populations", q.F("In a population in Hardy–Weinberg equilibrium, {0} % show the recessive phenotype. Frequency of carriers (Aa)?|En una población en equilibrio de Hardy-Weinberg, el {0} % muestra el fenotipo recesivo. ¿Frecuencia de portadores (Aa)?|Dans une population à l'équilibre de Hardy-Weinberg, {0} % présentent le phénotype récessif. Fréquence des porteurs (Aa) ?|En una població en equilibri de Hardy-Weinberg, el {0} % mostra el fenotip recessiu. Freqüència de portadors (Aa)?", q.N(qa * qa * 100)),
            2 * p * qa, [p * qa, qa, p * p, 2 * qa * qa], q.F("q = √{0} = {1}, p = {2}, carriers = 2pq = {3}.|q = √{0} = {1}, p = {2}, portadores = 2pq = {3}.|q = √{0} = {1}, p = {2}, porteurs = 2pq = {3}.|q = √{0} = {1}, p = {2}, portadors = 2pq = {3}.", q.N(qa * qa), q.N(qa), q.N(p), q.N(2 * p * qa)));
    }

    private static QuizQuestion Pcr(Mc q)
    {
        var start = q.Pick([1, 2, 5, 10]);
        var cycles = q.Between(3, 10);
        var copies = start * (1L << cycles);
        return q.Number("biology.molecular", q.F("A PCR starts with {0} copies and runs {1} perfect cycles. How many copies at the end?|Una PCR empieza con {0} copias y hace {1} ciclos perfectos. ¿Cuántas copias hay al final?|Une PCR démarre avec {0} copies et fait {1} cycles parfaits. Combien de copies à la fin ?|Una PCR comença amb {0} còpies i fa {1} cicles perfectes. Quantes còpies hi ha al final?", start, cycles),
            copies, [start * 2 * cycles, start * Math.Pow(cycles, 2), copies / 2, copies * 2], q.F("Each cycle doubles the DNA: {0} × 2^{1} = {2}.|Cada ciclo duplica el ADN: {0} × 2^{1} = {2}.|Chaque cycle double l'ADN : {0} × 2^{1} = {2}.|Cada cicle duplica l'ADN: {0} × 2^{1} = {2}.", start, cycles, copies), decimals: 0);
    }
}
