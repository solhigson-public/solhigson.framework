using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Solhigson.Utilities.Pluralization;

public class EnglishPluralizationService
{
    private readonly string[] _uninflectiveSuffixList = new string[7]
    {
        "fish",
        "ois",
        "sheep",
        "deer",
        "pos",
        "itis",
        "ism"
    };

    private readonly string[] _uninflectiveWordList = new string[75]
    {
        "bison",
        "flounder",
        "pliers",
        "bream",
        "gallows",
        "proceedings",
        "breeches",
        "graffiti",
        "rabies",
        "britches",
        "headquarters",
        "salmon",
        "carp",
        "herpes",
        "scissors",
        "chassis",
        "high-jinks",
        "sea-bass",
        "clippers",
        "homework",
        "series",
        "cod",
        "innings",
        "shears",
        "contretemps",
        "jackanapes",
        "species",
        "corps",
        "mackerel",
        "swine",
        "debris",
        "measles",
        "trout",
        "diabetes",
        "mews",
        "tuna",
        "djinn",
        "mumps",
        "whiting",
        "eland",
        "news",
        "wildebeest",
        "elk",
        "pincers",
        "police",
        "hair",
        "ice",
        "chaos",
        "milk",
        "cotton",
        "pneumonoultramicroscopicsilicovolcanoconiosis",
        "information",
        "aircraft",
        "scabies",
        "traffic",
        "corn",
        "millet",
        "rice",
        "hay",
        "hemp",
        "tobacco",
        "cabbage",
        "okra",
        "broccoli",
        "asparagus",
        "lettuce",
        "beef",
        "pork",
        "venison",
        "mutton",
        "cattle",
        "offspring",
        "molasses",
        "shambles",
        "shingles"
    };

    private Dictionary<string, string> _irregularVerbList = new Dictionary<string, string>()
    {
        {
            "am",
            "are"
        },
        {
            "are",
            "are"
        },
        {
            "is",
            "are"
        },
        {
            "was",
            "were"
        },
        {
            "were",
            "were"
        },
        {
            "has",
            "have"
        },
        {
            "have",
            "have"
        }
    };

    private List<string> _pronounList = new List<string>()
    {
        "I",
        "we",
        "you",
        "he",
        "she",
        "they",
        "it",
        "me",
        "us",
        "him",
        "her",
        "them",
        "myself",
        "ourselves",
        "yourself",
        "himself",
        "herself",
        "itself",
        "oneself",
        "oneselves",
        "my",
        "our",
        "your",
        "his",
        "their",
        "its",
        "mine",
        "yours",
        "hers",
        "theirs",
        "this",
        "that",
        "these",
        "those",
        "all",
        "another",
        "any",
        "anybody",
        "anyone",
        "anything",
        "both",
        "each",
        "other",
        "either",
        "everyone",
        "everybody",
        "everything",
        "most",
        "much",
        "nothing",
        "nobody",
        "none",
        "one",
        "others",
        "some",
        "somebody",
        "someone",
        "something",
        "what",
        "whatever",
        "which",
        "whichever",
        "who",
        "whoever",
        "whom",
        "whomever",
        "whose"
    };

    private Dictionary<string, string> _irregularPluralsDictionary = new Dictionary<string, string>()
    {
        {
            "brother",
            "brothers"
        },
        {
            "child",
            "children"
        },
        {
            "cow",
            "cows"
        },
        {
            "ephemeris",
            "ephemerides"
        },
        {
            "genie",
            "genies"
        },
        {
            "money",
            "moneys"
        },
        {
            "mongoose",
            "mongooses"
        },
        {
            "mythos",
            "mythoi"
        },
        {
            "octopus",
            "octopuses"
        },
        {
            "ox",
            "oxen"
        },
        {
            "soliloquy",
            "soliloquies"
        },
        {
            "trilby",
            "trilbys"
        },
        {
            "crisis",
            "crises"
        },
        {
            "synopsis",
            "synopses"
        },
        {
            "rose",
            "roses"
        },
        {
            "gas",
            "gases"
        },
        {
            "bus",
            "buses"
        },
        {
            "axis",
            "axes"
        },
        {
            "memo",
            "memos"
        },
        {
            "casino",
            "casinos"
        },
        {
            "silo",
            "silos"
        },
        {
            "stereo",
            "stereos"
        },
        {
            "studio",
            "studios"
        },
        {
            "lens",
            "lenses"
        },
        {
            "alias",
            "aliases"
        },
        {
            "pie",
            "pies"
        },
        {
            "corpus",
            "corpora"
        },
        {
            "viscus",
            "viscera"
        },
        {
            "hippopotamus",
            "hippopotami"
        },
        {
            "trace",
            "traces"
        },
        {
            "person",
            "people"
        },
        {
            "chili",
            "chilies"
        },
        {
            "analysis",
            "analyses"
        },
        {
            "basis",
            "bases"
        },
        {
            "neurosis",
            "neuroses"
        },
        {
            "oasis",
            "oases"
        },
        {
            "synthesis",
            "syntheses"
        },
        {
            "thesis",
            "theses"
        },
        {
            "change",
            "changes"
        },
        {
            "lie",
            "lies"
        },
        {
            "calorie",
            "calories"
        },
        {
            "freebie",
            "freebies"
        },
        {
            "case",
            "cases"
        },
        {
            "house",
            "houses"
        },
        {
            "valve",
            "valves"
        },
        {
            "cloth",
            "clothes"
        },
        {
            "tie",
            "ties"
        },
        {
            "movie",
            "movies"
        },
        {
            "bonus",
            "bonuses"
        },
        {
            "specimen",
            "specimens"
        }
    };

    private Dictionary<string, string> _assimilatedClassicalInflectionDictionary = new Dictionary<string, string>()
    {
        {
            "alumna",
            "alumnae"
        },
        {
            "alga",
            "algae"
        },
        {
            "vertebra",
            "vertebrae"
        },
        {
            "codex",
            "codices"
        },
        {
            "murex",
            "murices"
        },
        {
            "silex",
            "silices"
        },
        {
            "aphelion",
            "aphelia"
        },
        {
            "hyperbaton",
            "hyperbata"
        },
        {
            "perihelion",
            "perihelia"
        },
        {
            "asyndeton",
            "asyndeta"
        },
        {
            "noumenon",
            "noumena"
        },
        {
            "phenomenon",
            "phenomena"
        },
        {
            "criterion",
            "criteria"
        },
        {
            "organon",
            "organa"
        },
        {
            "prolegomenon",
            "prolegomena"
        },
        {
            "agendum",
            "agenda"
        },
        {
            "datum",
            "data"
        },
        {
            "extremum",
            "extrema"
        },
        {
            "bacterium",
            "bacteria"
        },
        {
            "desideratum",
            "desiderata"
        },
        {
            "stratum",
            "strata"
        },
        {
            "candelabrum",
            "candelabra"
        },
        {
            "erratum",
            "errata"
        },
        {
            "ovum",
            "ova"
        },
        {
            "forum",
            "fora"
        },
        {
            "addendum",
            "addenda"
        },
        {
            "stadium",
            "stadia"
        },
        {
            "automaton",
            "automata"
        },
        {
            "polyhedron",
            "polyhedra"
        }
    };

    private Dictionary<string, string> _oSuffixDictionary = new Dictionary<string, string>()
    {
        {
            "albino",
            "albinos"
        },
        {
            "generalissimo",
            "generalissimos"
        },
        {
            "manifesto",
            "manifestos"
        },
        {
            "archipelago",
            "archipelagos"
        },
        {
            "ghetto",
            "ghettos"
        },
        {
            "medico",
            "medicos"
        },
        {
            "armadillo",
            "armadillos"
        },
        {
            "guano",
            "guanos"
        },
        {
            "octavo",
            "octavos"
        },
        {
            "commando",
            "commandos"
        },
        {
            "inferno",
            "infernos"
        },
        {
            "photo",
            "photos"
        },
        {
            "ditto",
            "dittos"
        },
        {
            "jumbo",
            "jumbos"
        },
        {
            "pro",
            "pros"
        },
        {
            "dynamo",
            "dynamos"
        },
        {
            "lingo",
            "lingos"
        },
        {
            "quarto",
            "quartos"
        },
        {
            "embryo",
            "embryos"
        },
        {
            "lumbago",
            "lumbagos"
        },
        {
            "rhino",
            "rhinos"
        },
        {
            "fiasco",
            "fiascos"
        },
        {
            "magneto",
            "magnetos"
        },
        {
            "stylo",
            "stylos"
        }
    };

    private Dictionary<string, string> _classicalInflectionDictionary = new Dictionary<string, string>()
    {
        {
            "stamen",
            "stamina"
        },
        {
            "foramen",
            "foramina"
        },
        {
            "lumen",
            "lumina"
        },
        {
            "anathema",
            "anathemata"
        },
        {
            "enema",
            "enemata"
        },
        {
            "oedema",
            "oedemata"
        },
        {
            "bema",
            "bemata"
        },
        {
            "enigma",
            "enigmata"
        },
        {
            "sarcoma",
            "sarcomata"
        },
        {
            "carcinoma",
            "carcinomata"
        },
        {
            "gumma",
            "gummata"
        },
        {
            "schema",
            "schemata"
        },
        {
            "charisma",
            "charismata"
        },
        {
            "lemma",
            "lemmata"
        },
        {
            "soma",
            "somata"
        },
        {
            "diploma",
            "diplomata"
        },
        {
            "lymphoma",
            "lymphomata"
        },
        {
            "stigma",
            "stigmata"
        },
        {
            "dogma",
            "dogmata"
        },
        {
            "magma",
            "magmata"
        },
        {
            "stoma",
            "stomata"
        },
        {
            "drama",
            "dramata"
        },
        {
            "melisma",
            "melismata"
        },
        {
            "trauma",
            "traumata"
        },
        {
            "edema",
            "edemata"
        },
        {
            "miasma",
            "miasmata"
        },
        {
            "abscissa",
            "abscissae"
        },
        {
            "formula",
            "formulae"
        },
        {
            "medusa",
            "medusae"
        },
        {
            "amoeba",
            "amoebae"
        },
        {
            "hydra",
            "hydrae"
        },
        {
            "nebula",
            "nebulae"
        },
        {
            "antenna",
            "antennae"
        },
        {
            "hyperbola",
            "hyperbolae"
        },
        {
            "nova",
            "novae"
        },
        {
            "aurora",
            "aurorae"
        },
        {
            "lacuna",
            "lacunae"
        },
        {
            "parabola",
            "parabolae"
        },
        {
            "apex",
            "apices"
        },
        {
            "latex",
            "latices"
        },
        {
            "vertex",
            "vertices"
        },
        {
            "cortex",
            "cortices"
        },
        {
            "pontifex",
            "pontifices"
        },
        {
            "vortex",
            "vortices"
        },
        {
            "index",
            "indices"
        },
        {
            "simplex",
            "simplices"
        },
        {
            "iris",
            "irides"
        },
        {
            "clitoris",
            "clitorides"
        },
        {
            "alto",
            "alti"
        },
        {
            "contralto",
            "contralti"
        },
        {
            "soprano",
            "soprani"
        },
        {
            "basso",
            "bassi"
        },
        {
            "crescendo",
            "crescendi"
        },
        {
            "tempo",
            "tempi"
        },
        {
            "canto",
            "canti"
        },
        {
            "solo",
            "soli"
        },
        {
            "aquarium",
            "aquaria"
        },
        {
            "interregnum",
            "interregna"
        },
        {
            "quantum",
            "quanta"
        },
        {
            "compendium",
            "compendia"
        },
        {
            "lustrum",
            "lustra"
        },
        {
            "rostrum",
            "rostra"
        },
        {
            "consortium",
            "consortia"
        },
        {
            "maximum",
            "maxima"
        },
        {
            "spectrum",
            "spectra"
        },
        {
            "cranium",
            "crania"
        },
        {
            "medium",
            "media"
        },
        {
            "speculum",
            "specula"
        },
        {
            "curriculum",
            "curricula"
        },
        {
            "memorandum",
            "memoranda"
        },
        {
            "stadium",
            "stadia"
        },
        {
            "dictum",
            "dicta"
        },
        {
            "millenium",
            "millenia"
        },
        {
            "trapezium",
            "trapezia"
        },
        {
            "emporium",
            "emporia"
        },
        {
            "minimum",
            "minima"
        },
        {
            "ultimatum",
            "ultimata"
        },
        {
            "enconium",
            "enconia"
        },
        {
            "momentum",
            "momenta"
        },
        {
            "vacuum",
            "vacua"
        },
        {
            "gymnasium",
            "gymnasia"
        },
        {
            "optimum",
            "optima"
        },
        {
            "velum",
            "vela"
        },
        {
            "honorarium",
            "honoraria"
        },
        {
            "phylum",
            "phyla"
        },
        {
            "focus",
            "foci"
        },
        {
            "nimbus",
            "nimbi"
        },
        {
            "succubus",
            "succubi"
        },
        {
            "fungus",
            "fungi"
        },
        {
            "nucleolus",
            "nucleoli"
        },
        {
            "torus",
            "tori"
        },
        {
            "genius",
            "genii"
        },
        {
            "radius",
            "radii"
        },
        {
            "umbilicus",
            "umbilici"
        },
        {
            "incubus",
            "incubi"
        },
        {
            "stylus",
            "styli"
        },
        {
            "uterus",
            "uteri"
        },
        {
            "stimulus",
            "stimuli"
        },
        {
            "apparatus",
            "apparatus"
        },
        {
            "impetus",
            "impetus"
        },
        {
            "prospectus",
            "prospectus"
        },
        {
            "cantus",
            "cantus"
        },
        {
            "nexus",
            "nexus"
        },
        {
            "sinus",
            "sinus"
        },
        {
            "coitus",
            "coitus"
        },
        {
            "plexus",
            "plexus"
        },
        {
            "status",
            "status"
        },
        {
            "hiatus",
            "hiatus"
        },
        {
            "afreet",
            "afreeti"
        },
        {
            "afrit",
            "afriti"
        },
        {
            "efreet",
            "efreeti"
        },
        {
            "cherub",
            "cherubim"
        },
        {
            "goy",
            "goyim"
        },
        {
            "seraph",
            "seraphim"
        },
        {
            "alumnus",
            "alumni"
        }
    };

    private List<string> _knownConflictingPluralList = new List<string>()
    {
        "they",
        "them",
        "their",
        "have",
        "were",
        "yourself",
        "are"
    };

    private Dictionary<string, string> _wordsEndingWithSeDictionary = new Dictionary<string, string>()
    {
        {
            "house",
            "houses"
        },
        {
            "case",
            "cases"
        },
        {
            "enterprise",
            "enterprises"
        },
        {
            "purchase",
            "purchases"
        },
        {
            "surprise",
            "surprises"
        },
        {
            "release",
            "releases"
        },
        {
            "disease",
            "diseases"
        },
        {
            "promise",
            "promises"
        },
        {
            "refuse",
            "refuses"
        },
        {
            "whose",
            "whoses"
        },
        {
            "phase",
            "phases"
        },
        {
            "noise",
            "noises"
        },
        {
            "nurse",
            "nurses"
        },
        {
            "rose",
            "roses"
        },
        {
            "franchise",
            "franchises"
        },
        {
            "supervise",
            "supervises"
        },
        {
            "farmhouse",
            "farmhouses"
        },
        {
            "suitcase",
            "suitcases"
        },
        {
            "recourse",
            "recourses"
        },
        {
            "impulse",
            "impulses"
        },
        {
            "license",
            "licenses"
        },
        {
            "diocese",
            "dioceses"
        },
        {
            "excise",
            "excises"
        },
        {
            "demise",
            "demises"
        },
        {
            "blouse",
            "blouses"
        },
        {
            "bruise",
            "bruises"
        },
        {
            "misuse",
            "misuses"
        },
        {
            "curse",
            "curses"
        },
        {
            "prose",
            "proses"
        },
        {
            "purse",
            "purses"
        },
        {
            "goose",
            "gooses"
        },
        {
            "tease",
            "teases"
        },
        {
            "poise",
            "poises"
        },
        {
            "vase",
            "vases"
        },
        {
            "fuse",
            "fuses"
        },
        {
            "muse",
            "muses"
        },
        {
            "slaughterhouse",
            "slaughterhouses"
        },
        {
            "clearinghouse",
            "clearinghouses"
        },
        {
            "endonuclease",
            "endonucleases"
        },
        {
            "steeplechase",
            "steeplechases"
        },
        {
            "metamorphose",
            "metamorphoses"
        },
        {
            "intercourse",
            "intercourses"
        },
        {
            "commonsense",
            "commonsenses"
        },
        {
            "intersperse",
            "intersperses"
        },
        {
            "merchandise",
            "merchandises"
        },
        {
            "phosphatase",
            "phosphatases"
        },
        {
            "summerhouse",
            "summerhouses"
        },
        {
            "watercourse",
            "watercourses"
        },
        {
            "catchphrase",
            "catchphrases"
        },
        {
            "compromise",
            "compromises"
        },
        {
            "greenhouse",
            "greenhouses"
        },
        {
            "lighthouse",
            "lighthouses"
        },
        {
            "paraphrase",
            "paraphrases"
        },
        {
            "mayonnaise",
            "mayonnaises"
        },
        {
            "racecourse",
            "racecourses"
        },
        {
            "apocalypse",
            "apocalypses"
        },
        {
            "courthouse",
            "courthouses"
        },
        {
            "powerhouse",
            "powerhouses"
        },
        {
            "storehouse",
            "storehouses"
        },
        {
            "glasshouse",
            "glasshouses"
        },
        {
            "hypotenuse",
            "hypotenuses"
        },
        {
            "peroxidase",
            "peroxidases"
        },
        {
            "pillowcase",
            "pillowcases"
        },
        {
            "roundhouse",
            "roundhouses"
        },
        {
            "streetwise",
            "streetwises"
        },
        {
            "expertise",
            "expertises"
        },
        {
            "discourse",
            "discourses"
        },
        {
            "warehouse",
            "warehouses"
        },
        {
            "staircase",
            "staircases"
        },
        {
            "workhouse",
            "workhouses"
        },
        {
            "briefcase",
            "briefcases"
        },
        {
            "clubhouse",
            "clubhouses"
        },
        {
            "clockwise",
            "clockwises"
        },
        {
            "concourse",
            "concourses"
        },
        {
            "playhouse",
            "playhouses"
        },
        {
            "turquoise",
            "turquoises"
        },
        {
            "boathouse",
            "boathouses"
        },
        {
            "cellulose",
            "celluloses"
        },
        {
            "epitomise",
            "epitomises"
        },
        {
            "gatehouse",
            "gatehouses"
        },
        {
            "grandiose",
            "grandioses"
        },
        {
            "menopause",
            "menopauses"
        },
        {
            "penthouse",
            "penthouses"
        },
        {
            "racehorse",
            "racehorses"
        },
        {
            "transpose",
            "transposes"
        },
        {
            "almshouse",
            "almshouses"
        },
        {
            "customise",
            "customises"
        },
        {
            "footloose",
            "footlooses"
        },
        {
            "galvanise",
            "galvanises"
        },
        {
            "princesse",
            "princesses"
        },
        {
            "universe",
            "universes"
        },
        {
            "workhorse",
            "workhorses"
        }
    };

    private Dictionary<string, string> _wordsEndingWithSisDictionary = new Dictionary<string, string>()
    {
        {
            "analysis",
            "analyses"
        },
        {
            "crisis",
            "crises"
        },
        {
            "basis",
            "bases"
        },
        {
            "atherosclerosis",
            "atheroscleroses"
        },
        {
            "electrophoresis",
            "electrophoreses"
        },
        {
            "psychoanalysis",
            "psychoanalyses"
        },
        {
            "photosynthesis",
            "photosyntheses"
        },
        {
            "amniocentesis",
            "amniocenteses"
        },
        {
            "metamorphosis",
            "metamorphoses"
        },
        {
            "toxoplasmosis",
            "toxoplasmoses"
        },
        {
            "endometriosis",
            "endometrioses"
        },
        {
            "tuberculosis",
            "tuberculoses"
        },
        {
            "pathogenesis",
            "pathogeneses"
        },
        {
            "osteoporosis",
            "osteoporoses"
        },
        {
            "parenthesis",
            "parentheses"
        },
        {
            "anastomosis",
            "anastomoses"
        },
        {
            "peristalsis",
            "peristalses"
        },
        {
            "hypothesis",
            "hypotheses"
        },
        {
            "antithesis",
            "antitheses"
        },
        {
            "apotheosis",
            "apotheoses"
        },
        {
            "thrombosis",
            "thromboses"
        },
        {
            "diagnosis",
            "diagnoses"
        },
        {
            "synthesis",
            "syntheses"
        },
        {
            "paralysis",
            "paralyses"
        },
        {
            "prognosis",
            "prognoses"
        },
        {
            "cirrhosis",
            "cirrhoses"
        },
        {
            "sclerosis",
            "scleroses"
        },
        {
            "psychosis",
            "psychoses"
        },
        {
            "apoptosis",
            "apoptoses"
        },
        {
            "symbiosis",
            "symbioses"
        }
    };

    private Dictionary<string, string> _wordsEndingWithSusDictionary = new Dictionary<string, string>()
    {
        {
            "consensus",
            "consensuses"
        },
        {
            "census",
            "censuses"
        }
    };

    private Dictionary<string, string> _wordsEndingWithInxAnxYnxDictionary = new Dictionary<string, string>()
    {
        {
            "sphinx",
            "sphinxes"
        },
        {
            "larynx",
            "larynges"
        },
        {
            "lynx",
            "lynxes"
        },
        {
            "pharynx",
            "pharynxes"
        },
        {
            "phalanx",
            "phalanxes"
        }
    };

    private BidirectionalDictionary<string, string> _userDictionary;
    private StringBidirectionalDictionary _irregularPluralsPluralizationService;
    private StringBidirectionalDictionary _assimilatedClassicalInflectionPluralizationService;
    private StringBidirectionalDictionary _oSuffixPluralizationService;
    private StringBidirectionalDictionary _classicalInflectionPluralizationService;
    private StringBidirectionalDictionary _irregularVerbPluralizationService;
    private StringBidirectionalDictionary _wordsEndingWithSePluralizationService;
    private StringBidirectionalDictionary _wordsEndingWithSisPluralizationService;
    private StringBidirectionalDictionary _wordsEndingWithSusPluralizationService;
    private StringBidirectionalDictionary _wordsEndingWithInxAnxYnxPluralizationService;
    private List<string> _knownSingluarWords;
    private List<string> _knownPluralWords;

    private CultureInfo Culture;

    public EnglishPluralizationService()
    {
        this.Culture = new CultureInfo("en");
        this._userDictionary = new BidirectionalDictionary<string, string>();
        this._irregularPluralsPluralizationService =
            new StringBidirectionalDictionary(this._irregularPluralsDictionary);
        this._assimilatedClassicalInflectionPluralizationService =
            new StringBidirectionalDictionary(this._assimilatedClassicalInflectionDictionary);
        this._oSuffixPluralizationService = new StringBidirectionalDictionary(this._oSuffixDictionary);
        this._classicalInflectionPluralizationService =
            new StringBidirectionalDictionary(this._classicalInflectionDictionary);
        this._wordsEndingWithSePluralizationService =
            new StringBidirectionalDictionary(this._wordsEndingWithSeDictionary);
        this._wordsEndingWithSisPluralizationService =
            new StringBidirectionalDictionary(this._wordsEndingWithSisDictionary);
        this._wordsEndingWithSusPluralizationService =
            new StringBidirectionalDictionary(this._wordsEndingWithSusDictionary);
        this._wordsEndingWithInxAnxYnxPluralizationService =
            new StringBidirectionalDictionary(this._wordsEndingWithInxAnxYnxDictionary);
        this._irregularVerbPluralizationService = new StringBidirectionalDictionary(this._irregularVerbList);
        this._knownSingluarWords = new List<string>(Enumerable.Except<string>(
            Enumerable.Concat<string>(
                Enumerable.Concat<string>(
                    Enumerable.Concat<string>(
                        Enumerable.Concat<string>(
                            Enumerable.Concat<string>(
                                Enumerable.Concat<string>(
                                    Enumerable.Concat<string>(
                                        Enumerable.Concat<string>(
                                            Enumerable.Concat<string>(
                                                Enumerable.Concat<string>(
                                                    (IEnumerable<string>)this._irregularPluralsDictionary.Keys,
                                                    (IEnumerable<string>)this
                                                        ._assimilatedClassicalInflectionDictionary.Keys),
                                                (IEnumerable<string>)this._oSuffixDictionary.Keys),
                                            (IEnumerable<string>)this._classicalInflectionDictionary.Keys),
                                        (IEnumerable<string>)this._irregularVerbList.Keys),
                                    (IEnumerable<string>)this._irregularPluralsDictionary.Keys),
                                (IEnumerable<string>)this._wordsEndingWithSeDictionary.Keys),
                            (IEnumerable<string>)this._wordsEndingWithSisDictionary.Keys),
                        (IEnumerable<string>)this._wordsEndingWithSusDictionary.Keys),
                    (IEnumerable<string>)this._wordsEndingWithInxAnxYnxDictionary.Keys),
                (IEnumerable<string>)this._uninflectiveWordList),
            (IEnumerable<string>)this._knownConflictingPluralList));
        this._knownPluralWords = new List<string>(Enumerable.Concat<string>(
            Enumerable.Concat<string>(
                Enumerable.Concat<string>(
                    Enumerable.Concat<string>(
                        Enumerable.Concat<string>(
                            Enumerable.Concat<string>(
                                Enumerable.Concat<string>(
                                    Enumerable.Concat<string>(
                                        Enumerable.Concat<string>(
                                            Enumerable.Concat<string>(
                                                (IEnumerable<string>)this._irregularPluralsDictionary.Values,
                                                (IEnumerable<string>)this._assimilatedClassicalInflectionDictionary
                                                    .Values), (IEnumerable<string>)this._oSuffixDictionary.Values),
                                        (IEnumerable<string>)this._classicalInflectionDictionary.Values),
                                    (IEnumerable<string>)this._irregularVerbList.Values),
                                (IEnumerable<string>)this._irregularPluralsDictionary.Values),
                            (IEnumerable<string>)this._wordsEndingWithSeDictionary.Values),
                        (IEnumerable<string>)this._wordsEndingWithSisDictionary.Values),
                    (IEnumerable<string>)this._wordsEndingWithSusDictionary.Values),
                (IEnumerable<string>)this._wordsEndingWithInxAnxYnxDictionary.Values),
            (IEnumerable<string>)this._uninflectiveWordList));
    }

    public bool IsPlural(string word)
    {
        return this._userDictionary.ExistsInSecond(word) || !this._userDictionary.ExistsInFirst(word) &&
            (this.IsUninflective(word) || this._knownPluralWords.Contains(word.ToLower(this.Culture)) ||
             !this.Singularize(word).Equals(word));
    }

    public bool IsSingular(string word)
    {
        return this._userDictionary.ExistsInFirst(word) || !this._userDictionary.ExistsInSecond(word) &&
            (this.IsUninflective(word) || this._knownSingluarWords.Contains(word.ToLower(this.Culture)) ||
             !this.IsNoOpWord(word) && this.Singularize(word).Equals(word));
    }

    public string Pluralize(string word)
    {
        return this.Capitalize(word, new Func<string, string>(this.InternalPluralize));
    }

    private string InternalPluralize(string word)
    {
        if (this._userDictionary.ExistsInFirst(word))
            return this._userDictionary.GetSecondValue(word);
        if (this.IsNoOpWord(word))
            return word;
        string prefixWord;
        string suffixWord = this.GetSuffixWord(word, out prefixWord);
        if (this.IsNoOpWord(suffixWord) || this.IsUninflective(suffixWord) ||
            (this._knownPluralWords.Contains(suffixWord.ToLowerInvariant()) || this.IsPlural(suffixWord)))
            return prefixWord + suffixWord;
        if (this._irregularPluralsPluralizationService.ExistsInFirst(suffixWord))
            return prefixWord + this._irregularPluralsPluralizationService.GetSecondValue(suffixWord);
        string newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "man"
            }, (Func<string, string>)(s => s.Remove(s.Length - 2, 2) + "en"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "louse",
                "mouse"
            }, (Func<string, string>)(s => s.Remove(s.Length - 4, 4) + "ice"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "tooth"
            }, (Func<string, string>)(s => s.Remove(s.Length - 4, 4) + "eeth"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "goose"
            }, (Func<string, string>)(s => s.Remove(s.Length - 4, 4) + "eese"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "foot"
            }, (Func<string, string>)(s => s.Remove(s.Length - 3, 3) + "eet"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "zoon"
            }, (Func<string, string>)(s => s.Remove(s.Length - 3, 3) + "oa"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "cis",
                "sis",
                "xis"
            }, (Func<string, string>)(s => s.Remove(s.Length - 2, 2) + "es"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (this._assimilatedClassicalInflectionPluralizationService.ExistsInFirst(suffixWord))
            return prefixWord + this._assimilatedClassicalInflectionPluralizationService.GetSecondValue(suffixWord);
        if (this._classicalInflectionPluralizationService.ExistsInFirst(suffixWord))
            return prefixWord + this._classicalInflectionPluralizationService.GetSecondValue(suffixWord);
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "trix"
            }, (Func<string, string>)(s => s.Remove(s.Length - 1, 1) + "ces"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "eau",
                "ieu"
            }, (Func<string, string>)(s => s + "x"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (this._wordsEndingWithInxAnxYnxPluralizationService.ExistsInFirst(suffixWord))
            return prefixWord + this._wordsEndingWithInxAnxYnxPluralizationService.GetSecondValue(suffixWord);
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "ch",
                "sh",
                "ss"
            }, (Func<string, string>)(s => s + "es"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "alf",
                "elf",
                "olf",
                "eaf",
                "arf"
            }, (Func<string, string>)(s =>
            {
                if (!s.EndsWith("deaf", true, this.Culture))
                    return s.Remove(s.Length - 1, 1) + "ves";
                else
                    return s;
            }), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "nife",
                "life",
                "wife"
            }, (Func<string, string>)(s => s.Remove(s.Length - 2, 2) + "ves"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "ay",
                "ey",
                "iy",
                "oy",
                "uy"
            }, (Func<string, string>)(s => s + "s"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (suffixWord.EndsWith("y", true, this.Culture))
            return prefixWord + suffixWord.Remove(suffixWord.Length - 1, 1) + "ies";
        if (this._oSuffixPluralizationService.ExistsInFirst(suffixWord))
            return prefixWord + this._oSuffixPluralizationService.GetSecondValue(suffixWord);
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "ao",
                "eo",
                "io",
                "oo",
                "uo"
            }, (Func<string, string>)(s => s + "s"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (suffixWord.EndsWith("o", true, this.Culture) || suffixWord.EndsWith("s", true, this.Culture) ||
            suffixWord.EndsWith("x", true, this.Culture))
            return prefixWord + suffixWord + "es";
        else
            return prefixWord + suffixWord + "s";
    }

    public string Singularize(string word)
    {
        return this.Capitalize(word, new Func<string, string>(this.InternalSingularize));
    }

    private string InternalSingularize(string word)
    {
        if (this._userDictionary.ExistsInSecond(word))
            return this._userDictionary.GetFirstValue(word);
        if (this.IsNoOpWord(word))
            return word;
        string prefixWord;
        string suffixWord = this.GetSuffixWord(word, out prefixWord);
        if (this.IsNoOpWord(suffixWord) || this.IsUninflective(suffixWord) ||
            this._knownSingluarWords.Contains(suffixWord.ToLowerInvariant()))
            return prefixWord + suffixWord;
        if (this._irregularVerbPluralizationService.ExistsInSecond(suffixWord))
            return prefixWord + this._irregularVerbPluralizationService.GetFirstValue(suffixWord);
        if (this._irregularPluralsPluralizationService.ExistsInSecond(suffixWord))
            return prefixWord + this._irregularPluralsPluralizationService.GetFirstValue(suffixWord);
        if (this._wordsEndingWithSisPluralizationService.ExistsInSecond(suffixWord))
            return prefixWord + this._wordsEndingWithSisPluralizationService.GetFirstValue(suffixWord);
        if (this._wordsEndingWithSePluralizationService.ExistsInSecond(suffixWord))
            return prefixWord + this._wordsEndingWithSePluralizationService.GetFirstValue(suffixWord);
        if (this._wordsEndingWithSusPluralizationService.ExistsInSecond(suffixWord))
            return prefixWord + this._wordsEndingWithSusPluralizationService.GetFirstValue(suffixWord);
        string newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "men"
            }, (Func<string, string>)(s => s.Remove(s.Length - 2, 2) + "an"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "lice",
                "mice"
            }, (Func<string, string>)(s => s.Remove(s.Length - 3, 3) + "ouse"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "teeth"
            }, (Func<string, string>)(s => s.Remove(s.Length - 4, 4) + "ooth"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "geese"
            }, (Func<string, string>)(s => s.Remove(s.Length - 4, 4) + "oose"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "feet"
            }, (Func<string, string>)(s => s.Remove(s.Length - 3, 3) + "oot"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "zoa"
            }, (Func<string, string>)(s => s.Remove(s.Length - 2, 2) + "oon"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "ches",
                "shes",
                "sses"
            }, (Func<string, string>)(s => s.Remove(s.Length - 2, 2)), this.Culture, out newWord))
            return prefixWord + newWord;
        if (this._assimilatedClassicalInflectionPluralizationService.ExistsInSecond(suffixWord))
            return prefixWord + this._assimilatedClassicalInflectionPluralizationService.GetFirstValue(suffixWord);
        if (this._classicalInflectionPluralizationService.ExistsInSecond(suffixWord))
            return prefixWord + this._classicalInflectionPluralizationService.GetFirstValue(suffixWord);
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "trices"
            }, (Func<string, string>)(s => s.Remove(s.Length - 3, 3) + "x"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "eaux",
                "ieux"
            }, (Func<string, string>)(s => s.Remove(s.Length - 1, 1)), this.Culture, out newWord))
            return prefixWord + newWord;
        if (this._wordsEndingWithInxAnxYnxPluralizationService.ExistsInSecond(suffixWord))
            return prefixWord + this._wordsEndingWithInxAnxYnxPluralizationService.GetFirstValue(suffixWord);
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "alves",
                "elves",
                "olves",
                "eaves",
                "arves"
            }, (Func<string, string>)(s => s.Remove(s.Length - 3, 3) + "f"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "nives",
                "lives",
                "wives"
            }, (Func<string, string>)(s => s.Remove(s.Length - 3, 3) + "fe"), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "ays",
                "eys",
                "iys",
                "oys",
                "uys"
            }, (Func<string, string>)(s => s.Remove(s.Length - 1, 1)), this.Culture, out newWord))
            return prefixWord + newWord;
        if (suffixWord.EndsWith("ies", true, this.Culture))
            return prefixWord + suffixWord.Remove(suffixWord.Length - 3, 3) + "y";
        if (this._oSuffixPluralizationService.ExistsInSecond(suffixWord))
            return prefixWord + this._oSuffixPluralizationService.GetFirstValue(suffixWord);
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "aos",
                "eos",
                "ios",
                "oos",
                "uos"
            }, (Func<string, string>)(s => suffixWord.Remove(suffixWord.Length - 1, 1)), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "ces"
            }, (Func<string, string>)(s => s.Remove(s.Length - 1, 1)), this.Culture, out newWord))
            return prefixWord + newWord;
        if (PluralizationServiceUtil.TryInflectOnSuffixInWord(suffixWord, (IEnumerable<string>)new List<string>()
            {
                "ces",
                "ses",
                "xes"
            }, (Func<string, string>)(s => s.Remove(s.Length - 2, 2)), this.Culture, out newWord))
            return prefixWord + newWord;
        if (suffixWord.EndsWith("oes", true, this.Culture))
            return prefixWord + suffixWord.Remove(suffixWord.Length - 2, 2);
        if (suffixWord.EndsWith("ss", true, this.Culture) || !suffixWord.EndsWith("s", true, this.Culture))
            return prefixWord + suffixWord;
        else
            return prefixWord + suffixWord.Remove(suffixWord.Length - 1, 1);
    }

    private string Capitalize(string word, Func<string, string> action)
    {
        string str = action(word);
        if (!this.IsCapitalized(word) || str.Length == 0)
            return str;
        StringBuilder stringBuilder = new StringBuilder(str.Length);
        stringBuilder.Append(char.ToUpperInvariant(str[0]));
        stringBuilder.Append(str.Substring(1));
        return ((object)stringBuilder).ToString();
    }

    public string Capitalize(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return string.Empty;
        }

        if (!this.IsCapitalized(str) || str.Length == 0)
            return str;
        StringBuilder stringBuilder = new StringBuilder(str.Length);
        stringBuilder.Append(char.ToUpperInvariant(str[0]));
        stringBuilder.Append(str.Substring(1));
        return ((object)stringBuilder).ToString();
    }

    private string GetSuffixWord(string word, out string prefixWord)
    {
        int num = word.LastIndexOf(' ');
        prefixWord = word.Substring(0, num + 1);
        return word.Substring(num + 1);
    }

    private bool IsCapitalized(string word)
    {
        if (!string.IsNullOrEmpty(word))
            return char.IsUpper(word, 0);
        else
            return false;
    }

    private bool IsAlphabets(string word)
    {
        return !string.IsNullOrEmpty(word.Trim()) && word.Equals(word.Trim()) &&
               !Regex.IsMatch(word, "[^a-zA-Z\\s]");
    }

    private bool IsUninflective(string word)
    {
        return PluralizationServiceUtil.DoesWordContainSuffix(word,
                   (IEnumerable<string>)this._uninflectiveSuffixList, this.Culture) ||
               !word.ToLower(this.Culture).Equals(word) && word.EndsWith("ese", false, this.Culture) ||
               Enumerable.Contains<string>((IEnumerable<string>)this._uninflectiveWordList,
                   word.ToLowerInvariant());
    }

    private bool IsNoOpWord(string word)
    {
        return !this.IsAlphabets(word) || word.Length <= 1 || this._pronounList.Contains(word.ToLowerInvariant());
    }
}

internal static class PluralizationServiceUtil
{
    internal static bool DoesWordContainSuffix(string word, IEnumerable<string> suffixes, CultureInfo culture)
    {
        return suffixes.Any((Func<string, bool>)(s => word.EndsWith(s, true, culture)));
    }

    internal static bool TryInflectOnSuffixInWord(string word, IEnumerable<string> suffixes,
        Func<string, string> operationOnWord, CultureInfo culture, out string newWord)
    {
        newWord = (string)null;
        if (!PluralizationServiceUtil.DoesWordContainSuffix(word, suffixes, culture))
            return false;
        newWord = operationOnWord(word);
        return true;
    }
}