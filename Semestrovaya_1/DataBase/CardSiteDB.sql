\restrict bynV66OKeFnRtUyNL4h9Fm4uvDhZEH9YHNRQNoc8vGLzVCydEgh1INKevzT6JQ5

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

CREATE TABLE public.admins (
    id integer NOT NULL,
    username character varying(50) NOT NULL,
    passwordhash character varying(255) NOT NULL,
    email character varying(100) NOT NULL,
    createdat timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.admins OWNER TO postgres;

CREATE SEQUENCE public.admins_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.admins_id_seq OWNER TO postgres;

ALTER SEQUENCE public.admins_id_seq OWNED BY public.admins.id;


CREATE TABLE public.cardcolors (
    id integer NOT NULL,
    name character varying(50) NOT NULL
);


ALTER TABLE public.cardcolors OWNER TO postgres;

CREATE SEQUENCE public.cardcolors_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.cardcolors_id_seq OWNER TO postgres;

ALTER SEQUENCE public.cardcolors_id_seq OWNED BY public.cardcolors.id;


CREATE TABLE public.cardproducts (
    cardid integer NOT NULL,
    productid integer NOT NULL
);


ALTER TABLE public.cardproducts OWNER TO postgres;

CREATE TABLE public.cardrarities (
    id integer NOT NULL,
    code character varying(10) NOT NULL
);


ALTER TABLE public.cardrarities OWNER TO postgres;

CREATE SEQUENCE public.cardrarities_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.cardrarities_id_seq OWNER TO postgres;

ALTER SEQUENCE public.cardrarities_id_seq OWNED BY public.cardrarities.id;


CREATE TABLE public.cards (
    id integer NOT NULL,
    name character varying(150) NOT NULL,
    cardnumber character varying(30) NOT NULL,
    imageurl character varying(255) NOT NULL,
    cost integer,
    specifiedcost character varying(50),
    power integer,
    combopower integer,
    skills text,
    setid integer NOT NULL,
    typeid integer NOT NULL,
    rarityid integer NOT NULL,
    colorid integer NOT NULL,
    CONSTRAINT cards_combopower_check CHECK (((combopower >= 0) AND (combopower <= 10000))),
    CONSTRAINT cards_cost_check CHECK (((cost >= 0) AND (cost <= 9))),
    CONSTRAINT cards_power_check CHECK (((power >= 0) AND (power <= 55000)))
);


ALTER TABLE public.cards OWNER TO postgres;

CREATE SEQUENCE public.cards_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.cards_id_seq OWNER TO postgres;

ALTER SEQUENCE public.cards_id_seq OWNED BY public.cards.id;


CREATE TABLE public.cardsets (
    id integer NOT NULL,
    name character varying(150) NOT NULL,
    code character varying(20) NOT NULL
);


ALTER TABLE public.cardsets OWNER TO postgres;

CREATE SEQUENCE public.cardsets_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.cardsets_id_seq OWNER TO postgres;

ALTER SEQUENCE public.cardsets_id_seq OWNED BY public.cardsets.id;


CREATE TABLE public.cardspecialtraits (
    cardid integer NOT NULL,
    specialtraitid integer NOT NULL
);


ALTER TABLE public.cardspecialtraits OWNER TO postgres;

CREATE TABLE public.cardtypes (
    id integer NOT NULL,
    name character varying(50) NOT NULL
);


ALTER TABLE public.cardtypes OWNER TO postgres;

CREATE SEQUENCE public.cardtypes_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.cardtypes_id_seq OWNER TO postgres;

ALTER SEQUENCE public.cardtypes_id_seq OWNED BY public.cardtypes.id;


CREATE TABLE public.products (
    id integer NOT NULL,
    name character varying(150) NOT NULL,
    imageurl character varying(255) NOT NULL,
    producturl character varying(255)
);


ALTER TABLE public.products OWNER TO postgres;

CREATE SEQUENCE public.products_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.products_id_seq OWNER TO postgres;

ALTER SEQUENCE public.products_id_seq OWNED BY public.products.id;


CREATE TABLE public.specialtraits (
    id integer NOT NULL,
    name character varying(100) NOT NULL
);


ALTER TABLE public.specialtraits OWNER TO postgres;

CREATE SEQUENCE public.specialtraits_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.specialtraits_id_seq OWNER TO postgres;

ALTER SEQUENCE public.specialtraits_id_seq OWNED BY public.specialtraits.id;


ALTER TABLE ONLY public.admins ALTER COLUMN id SET DEFAULT nextval('public.admins_id_seq'::regclass);


ALTER TABLE ONLY public.cardcolors ALTER COLUMN id SET DEFAULT nextval('public.cardcolors_id_seq'::regclass);


ALTER TABLE ONLY public.cardrarities ALTER COLUMN id SET DEFAULT nextval('public.cardrarities_id_seq'::regclass);


ALTER TABLE ONLY public.cards ALTER COLUMN id SET DEFAULT nextval('public.cards_id_seq'::regclass);


ALTER TABLE ONLY public.cardsets ALTER COLUMN id SET DEFAULT nextval('public.cardsets_id_seq'::regclass);


ALTER TABLE ONLY public.cardtypes ALTER COLUMN id SET DEFAULT nextval('public.cardtypes_id_seq'::regclass);


ALTER TABLE ONLY public.products ALTER COLUMN id SET DEFAULT nextval('public.products_id_seq'::regclass);


ALTER TABLE ONLY public.specialtraits ALTER COLUMN id SET DEFAULT nextval('public.specialtraits_id_seq'::regclass);


COPY public.admins (id, username, passwordhash, email, createdat) FROM stdin;
\.


COPY public.cardcolors (id, name) FROM stdin;
1	Red
2	Blue
3	Green
4	Yellow
5	Black
\.


COPY public.cardproducts (cardid, productid) FROM stdin;
\.


COPY public.cardrarities (id, code) FROM stdin;
1	L
2	C
3	UC
4	R
5	SR
6	SCR
7	PR
\.


COPY public.cards (id, name, cardnumber, imageurl, cost, specifiedcost, power, combopower, skills, setid, typeid, rarityid, colorid) FROM stdin;
1	Son Goku	FB11-001	/images/cards/FB11/FB11-001.webp	\N	\N	15000	\N	[When Attacking] Draw 1 card.\n[Awaken] (Can be activated during [Activate Main/Battle] if the conditions are met.) When your life is at 4 or less, or you have 2 or more energy and a card with [Evolve] in your Battle Area, flip this card over.	1	1	4	1
2	Son Goku	FB11-001	/images/cards/FB11/FB11-001(2).webp	\N	\N	15000	\N	[When Attacking] Draw 1 card.\n[Awaken] (Can be activated during [Activate Main/Battle] if the conditions are met.) When your life is at 4 or less, or you have 2 or more energy and a card with [Evolve] in your Battle Area, flip this card over.	1	1	4	1
3	Great Saiyaman	FB11-002	/images/cards/FB11/FB11-002.webp	3	RR	20000	\N	[On Play] If your Leader is a 《Saiyan》-only <Son Goku>, play up to 1 Battle Card with a cost of 1 and 《Saiyan》 or 《Earthling》 in its special traits from your hand. Then, during this turn, reduce the cost of the next [Evolve] skill you activate from your hand by (2).\n[When Attacking] This card gets +5000 power for the battle.	1	2	5	1
4	Great Saiyaman	FB11-002	/images/cards/FB11/FB11-002(2).webp	3	RR	20000	\N	[On Play] If your Leader is a 《Saiyan》-only <Son Goku>, play up to 1 Battle Card with a cost of 1 and 《Saiyan》 or 《Earthling》 in its special traits from your hand. Then, during this turn, reduce the cost of the next [Evolve] skill you activate from your hand by (2).\n[When Attacking] This card gets +5000 power for the battle.	1	2	5	1
5	Great Saiyaman 2	FB11-003	/images/cards/FB11/FB11-003.webp	1	R	10000	10000	[When Attacking] If you have a <Son Gohan : Adolescence> or <Great Saiyaman>―both with a cost of 3 or more―in your Battle Area, choose up to 1 <Son Gohan : Adolescence> or up to 1 card with <Great Saiyaman> in its card name in your Battle Area and it gets +10000 power for the turn.	1	2	4	1
6	Great Saiyaman 2	FB11-004	/images/cards/FB11/FB11-003.webp	7	R	15000	10000	[Super Combo] (A maximum of 4 cards with [Super Combo] can be placed in a deck.)\n[Auto][Opponent's Turn] When you use this card in a combo from your hand, this card gets +10000 combo power.	1	2	3	1
7	Great Saiyaman 2	FB11-004	/images/cards/FB11/FB11-004(2).webp	7	R	15000	10000	[Super Combo] (A maximum of 4 cards with [Super Combo] can be placed in a deck.)\n[Auto][Opponent's Turn] When you use this card in a combo from your hand, this card gets +10000 combo power.	1	2	3	1
8	Gogeta : BR	FB11-005	/images/cards/FB11/FB11-005.webp	5	R	25000	10000	[Fusion Evolve] (You may pay the skill cost and play on top of the specified cards in a Battle Area in Active Mode)(0): <Son Goku : BR> and <Vegeta : BR> with equal power of 20000 or more.\n[Permanent] This card in your Battle Area and your <Gogeta : BR> Leader get +15000 power while they are in a battle with an opponent's card with an original power of 30000.	1	2	3	1
9	Gotenks	FB11-006	/images/cards/FB11/FB11-006.webp	4	R	30000	10000	\N	1	2	2	1
10	Son Goku	FB11-007	/images/cards/FB11/FB11-007.webp	1	R	15000	10000	\N	1	2	2	1
11	Son Goku	FB11-008	/images/cards/FB11/FB11-008.webp	1	R	5000	10000	[On Play] Draw 2 cards. Then, discard 2 cards from your hand.	1	2	2	1
12	Son Goku	FB11-009	/images/cards/FB11/FB11-009.webp	1	R	5000	5000	[On Play] If your Leader is <Son Goku>, look at 3 cards from the top of your deck, reveal up to 1 <Son Goku> or card with 《Earthling》 in its special traits and add it to your hand. Place the rest at the bottom of their owner's deck in a random order.	1	2	4	1
13	Son Goku	FB11-009	/images/cards/FB11/FB11-009(2).webp	1	R	5000	5000	[On Play] If your Leader is <Son Goku>, look at 3 cards from the top of your deck, reveal up to 1 <Son Goku> or card with 《Earthling》 in its special traits and add it to your hand. Place the rest at the bottom of their owner's deck in a random order.	1	2	4	1
14	Son Goku	FB11-010	/images/cards/FB11/FB11-010.webp	2	R	20000	\N	[On Play] Choose all Battle Cards with costs of 1 or less in your opponent's Battle Area and they get -10000 power for the turn.	1	2	2	1
15	Son Goku	FB11-011	/images/cards/FB11/FB11-011.webp	4	RR	30000	5000	[Evolve] (You may pay the skill cost and play on top of the specified card in a Battle Area)(1): <Son Goku>\n[Auto][Once Per Turn] When this card wins a battle, look at 2 cards from the top of your deck and add 1 card to your hand. Place the rest at the bottom of its owner's deck.	1	2	5	1
16	Son Goku	FB11-011	/images/cards/FB11/FB11-011(2).webp	4	RR	30000	5000	[Evolve] (You may pay the skill cost and play on top of the specified card in a Battle Area)(1): <Son Goku>\n[Auto][Once Per Turn] When this card wins a battle, look at 2 cards from the top of your deck and add 1 card to your hand. Place the rest at the bottom of its owner's deck.	1	2	5	1
17	Son Goku : BR	FB11-012	/images/cards/FB11/FB11-012.webp	2	R	20000	5000	[On Play](1): If your Leader is <Gogeta : BR>, play up to 1 <Vegeta : BR> with a cost of 2 or less from your hand in Rest Mode.\n\n[When Attacking] Add up to 1 card from your life to your hand.	1	2	3	1
18	Son Goten	FB11-013	/images/cards/FB11/FB11-013.webp	1	R	5000	10000	[On Play] Choose up to 1 Battle Card in your opponent's Battle Area and it gets -5000 power for the turn.	1	2	2	1
19	Son Goten	FB11-014	/images/cards/FB11/FB11-014.webp	3	R	20000	5000	[Evolve] (You may pay the skill cost and play on top of the specified card in a Battle Area)(1): <Son Goten>\n[On Play] Play up to 1 <Trunks : Youth> with a cost of 2 or less from your hand.	1	2	3	1
20	Son Gohan : Adolescence	FB11-015	/images/cards/FB11/FB11-015.webp	1	R	5000	5000	[On Play] Draw 1 card.	1	2	2	1
21	Son Gohan : Adolescence	FB11-016	/images/cards/FB11/FB11-016.webp	5	R	30000	5000	[Evolve] (You may pay the skill cost and play on top of the specified card in a Battle Area)(2): <Son Gohan : Adolescence> or <Great Saiyaman>\n[On Play] Choose up to 1 Battle Card with an original power of 10000 and 《Earthling》 in its special traits in your Battle Area, and switch this card and the chosen card to Active Mode.	1	2	4	1
22	Son Gohan : Adolescence	FB11-016	/images/cards/FB11/FB11-016(2).webp	5	R	30000	5000	[Evolve] (You may pay the skill cost and play on top of the specified card in a Battle Area)(2): <Son Gohan : Adolescence> or <Great Saiyaman>\n[On Play] Choose up to 1 Battle Card with an original power of 10000 and 《Earthling》 in its special traits in your Battle Area, and switch this card and the chosen card to Active Mode.	1	2	4	1
23	Trunks : Youth	FB11-017	/images/cards/FB11/FB11-017.webp	1	R	10000	5000	[Activate Battle](1), place this card from a Battle Area into its owner's Drop: If your Leader is <Son Goku>, your card with an original power of 30000 or more and [Evolve] that's in a battle gains [Double Strike] and +5000 power for the battle.\n([Double Strike]: This card inflicts 2 damage instead of 1 when attacking.)	1	2	5	1
24	Trunks : Youth	FB11-017	/images/cards/FB11/FB11-017(2).webp	1	R	10000	5000	[Activate Battle](1), place this card from a Battle Area into its owner's Drop: If your Leader is <Son Goku>, your card with an original power of 30000 or more and [Evolve] that's in a battle gains [Double Strike] and +5000 power for the battle.\n([Double Strike]: This card inflicts 2 damage instead of 1 when attacking.)	1	2	5	1
25	Trunks : Youth	FB11-018	/images/cards/FB11/FB11-018.webp	2	R	20000	5000	[Activate Main] Place this card and 1 <Son Goten>―both with 20000 power―from your Battle Area into their owner's Drop: Play up to 1 <Gotenks> with a cost of 4 from your hand.	1	2	3	1
26	Videl	FB11-019	/images/cards/FB11/FB11-019.webp	1	R	15000	5000	[Activate Main] Place this card from a Battle Area into its owner's Drop: If your Leader is awakened, play up to 1 <Great Saiyaman 2> with a cost of 1 from your hand.	1	2	2	1
27	Bulma	FB11-020	/images/cards/FB11/FB11-020.webp	1	R	5000	5000	[On Play] Choose up to 1 Battle Card in your opponent's Battle Area and it gets -5000 power for the turn.\n\n[Activate Main] Place this card from a Battle Area into its owner's Drop: If your Leader is <Son Goku> and you have a card with [Evolve] in your Battle Area, choose up to 1 Battle Card in your opponent's Battle Area and it gets -10000 power for the turn.	1	2	4	1
28	Vegeta	FB11-021	/images/cards/FB11/FB11-021.webp	1	R	15000	10000	[On Play] Choose up to 1 Battle Card in your opponent's Battle Area and it gets -5000 power for the turn.\n\n[Activate Main] Place this card from a Battle Area into its owner's Drop: If your Leader is <Son Goku> and you have a card with [Evolve] in your Battle Area, choose up to 1 Battle Card in your opponent's Battle Area and it gets -10000 power for the turn.	1	2	2	1
29	Vegeta	FB11-022	/images/cards/FB11/FB11-022.webp	3	R	25000	5000	[Evolve] (You may pay the skill cost and play on top of the specified card in a Battle Area)(1): <Vegeta>\n[When Attacking] Add 1 card from your life to your hand: This card gains [Critical] and +5000 power for the battle.\n([Critical]: When this card inflicts damage with an attack, the life is placed in the Drop.)	1	2	3	1
30	Vegeta : BR	FB11-023	/images/cards/FB11/FB11-023.webp	2	R	15000	10000	[Permanent][Your Turn] This card in a Battle Area gets +5000 power.\n[On Play] Choose 1 of your awakened <Gogeta : BR> Leaders and its original power becomes 25000 until the end of your opponent's next turn.	1	2	4	1
31	Entrusted Sword	FB11-024	/images/cards/FB11/FB11-024.webp	1	R	\N	\N	[Activate Main] If your Leader is awakened and you have a <Trunks : Youth> in your Battle Area, draw 2 cards.	1	3	3	1
32	Trunks : Future/Son Gohan : Future	FB11-025	/images/cards/FB11/FB11-025.webp	\N	\N	15000	\N	[When Attacking] Draw 1 card.\n[Awaken] (Can be activated during [Activate Main/Battle] if the conditions are met.) If you have a total of 2 or more [Ki] in your Battle Area, draw 1 card. Then, flip this card over.\n([Ki]: An energy marker placed under a card.)	1	1	1	2
33	Trunks : Future/Son Gohan : Future	FB11-025	/images/cards/FB11/FB11-025(2).webp	\N	\N	15000	\N	[When Attacking] Draw 1 card.\n[Awaken] (Can be activated during [Activate Main/Battle] if the conditions are met.) If you have a total of 2 or more [Ki] in your Battle Area, draw 1 card. Then, flip this card over.\n([Ki]: An energy marker placed under a card.)	1	1	1	2
34	Android 17	FB11-026	/images/cards/FB11/FB11-026.webp	2	U	10000	10000	[On Play] If your Battle Card was removed from a Battle Area by one of your skills during this turn, draw 2 cards. Then, discard 1 card from your hand.	1	2	3	2
35	Android 18	FB11-027	/images/cards/FB11/FB11-027.webp	1	U	5000	10000	[On Play] Choose up to 1 Battle Card with a cost of 1 or less in your opponent's Battle Area and place it at the bottom of its owner's deck.	1	2	3	2
36	Son Goku	FB11-028	/images/cards/FB11/FB11-028.webp	2	U	20000	10000	\N	1	2	2	2
37	Son Goku	FB11-029	/images/cards/FB11/FB11-029.webp	3	U	20000	10000	[Permanent] If you have 12 or more cards in your Drop, reduce the cost of this card in your hand by 2.\n[On Play] Choose up to 1 Battle Card with a cost of 2 or less in your opponent's Battle Area, place it at the bottom of its owner's deck, and discard 2 cards from your hand. Then, you can't play <Son Goku> with a cost of 3 or less for the turn.	1	2	4	2
38	Son Gohan : Future	FB11-030	/images/cards/FB11/FB11-030.webp	2	U	20000	5000	[When Attacking] Choose up to 1 Battle Card in your Battle Area and it gets +1 [Ki].\n([Ki]: An energy marker placed under a card.)	1	2	3	2
39	Son Gohan : Future	FB11-031	/images/cards/FB11/FB11-031.webp	7	U	15000	10000	[Super Combo] (A maximum of 4 cards with [Super Combo] can be placed in a deck.)\n[Auto][Opponent's Turn] When you use this card in a combo from your hand, this card gets +10000 combo power.	1	2	3	2
40	Son Gohan : Future	FB11-031	/images/cards/FB11/FB11-031(2).webp	7	U	15000	10000	[Super Combo] (A maximum of 4 cards with [Super Combo] can be placed in a deck.)\n[Auto][Opponent's Turn] When you use this card in a combo from your hand, this card gets +10000 combo power.	1	2	3	2
\.


COPY public.cardsets (id, name, code) FROM stdin;
1	BOOSTER PACK -BRIGHTNESS OF HOPE-	FB11
3	STORY BOOSTER 01	ST01
\.


COPY public.cardspecialtraits (cardid, specialtraitid) FROM stdin;
1	10
2	10
3	10
3	9
4	10
4	9
5	9
6	9
7	9
8	10
9	10
9	9
10	10
11	10
12	10
13	10
14	10
15	10
16	10
17	10
18	10
18	9
19	10
19	9
20	10
20	9
21	10
21	9
22	10
22	9
23	10
23	9
24	10
24	9
25	10
25	9
26	9
27	9
28	10
29	10
30	10
31	10
31	9
32	10
32	9
33	10
33	9
34	18
35	18
36	10
37	10
37	5
37	61
38	10
38	9
39	10
39	9
40	10
40	9
\.


COPY public.cardtypes (id, name) FROM stdin;
1	LEADER
2	BATTLE
3	EXTRA
4	ENERGY MARKER
\.


COPY public.products (id, name, imageurl, producturl) FROM stdin;
\.


COPY public.specialtraits (id, name) FROM stdin;
1	Universe 2
2	Universe 3
3	Universe 4
4	Universe 6
5	Universe 7
6	Universe 9
7	Universe 10
8	Universe 11
9	Earthling
10	Saiyan
11	Great Ape
12	Robot
13	God
14	Demon Clan
15	Namekian
16	Alien
17	Frieza Clan
18	Android
19	Kai
20	Supreme Kai
21	Attendant
22	God of Destruction
23	Angel
24	Saibaiman
25	Animal
26	Red Ribbon Army
27	Frieza's Army
28	Chilled's Army
29	Ginyu Force
30	Cooler's Armored Squadron
31	Bardock's Crew
32	Turles Crusher Corps
33	Slug's Gang
34	Boujack Brigade
35	Evil Wizard
36	Majin
37	Shadow Dragon
38	Maiden Squadron
39	Policeman
40	Trio De Dangers
41	Pride Troopers
42	Gendarmerie Force
43	Machine Mutant
44	Korin Tower
45	Lookout
46	Satan City
47	Universe
48	Another World
49	King Kai's Planet
50	World of Void
51	Demon Realm
52	Planet Namek
53	Jaguar's Island
54	Planet Vampa
55	Demon Realm Race
56	First Demon World
57	Second Demon World
58	Third Demon World
59	World Tournament
60	Another World Budokai
61	Tournament of Power
62	Training
63	Brainwashed
64	Absorption
65	Wicked Soul
66	Dragon Ball
67	Shenron
68	Item
69	Potara
70	Adventure
71	Greatest Showdown
72	Goku's Lineage
73	Master's Teachings
\.


SELECT pg_catalog.setval('public.admins_id_seq', 1, false);


SELECT pg_catalog.setval('public.cardcolors_id_seq', 5, true);


SELECT pg_catalog.setval('public.cardrarities_id_seq', 7, true);


SELECT pg_catalog.setval('public.cards_id_seq', 40, true);


SELECT pg_catalog.setval('public.cardsets_id_seq', 3, true);


SELECT pg_catalog.setval('public.cardtypes_id_seq', 4, true);


SELECT pg_catalog.setval('public.products_id_seq', 1, false);


SELECT pg_catalog.setval('public.specialtraits_id_seq', 74, true);


ALTER TABLE ONLY public.admins
    ADD CONSTRAINT admins_email_key UNIQUE (email);


ALTER TABLE ONLY public.admins
    ADD CONSTRAINT admins_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.admins
    ADD CONSTRAINT admins_username_key UNIQUE (username);


ALTER TABLE ONLY public.cardcolors
    ADD CONSTRAINT cardcolors_name_key UNIQUE (name);


ALTER TABLE ONLY public.cardcolors
    ADD CONSTRAINT cardcolors_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.cardproducts
    ADD CONSTRAINT cardproducts_pkey PRIMARY KEY (cardid, productid);


ALTER TABLE ONLY public.cardrarities
    ADD CONSTRAINT cardrarities_code_key UNIQUE (code);


ALTER TABLE ONLY public.cardrarities
    ADD CONSTRAINT cardrarities_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.cards
    ADD CONSTRAINT cards_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.cardsets
    ADD CONSTRAINT cardsets_code_key UNIQUE (code);


ALTER TABLE ONLY public.cardsets
    ADD CONSTRAINT cardsets_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.cardspecialtraits
    ADD CONSTRAINT cardspecialtraits_pkey PRIMARY KEY (cardid, specialtraitid);


ALTER TABLE ONLY public.cardtypes
    ADD CONSTRAINT cardtypes_name_key UNIQUE (name);


ALTER TABLE ONLY public.cardtypes
    ADD CONSTRAINT cardtypes_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.products
    ADD CONSTRAINT products_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.specialtraits
    ADD CONSTRAINT specialtraits_name_key UNIQUE (name);


ALTER TABLE ONLY public.specialtraits
    ADD CONSTRAINT specialtraits_pkey PRIMARY KEY (id);


ALTER TABLE ONLY public.cardproducts
    ADD CONSTRAINT cardproducts_cardid_fkey FOREIGN KEY (cardid) REFERENCES public.cards(id) ON DELETE CASCADE;


ALTER TABLE ONLY public.cardproducts
    ADD CONSTRAINT cardproducts_productid_fkey FOREIGN KEY (productid) REFERENCES public.products(id);


ALTER TABLE ONLY public.cards
    ADD CONSTRAINT cards_colorid_fkey FOREIGN KEY (colorid) REFERENCES public.cardcolors(id);


ALTER TABLE ONLY public.cards
    ADD CONSTRAINT cards_rarityid_fkey FOREIGN KEY (rarityid) REFERENCES public.cardrarities(id);


ALTER TABLE ONLY public.cards
    ADD CONSTRAINT cards_setid_fkey FOREIGN KEY (setid) REFERENCES public.cardsets(id);


ALTER TABLE ONLY public.cards
    ADD CONSTRAINT cards_typeid_fkey FOREIGN KEY (typeid) REFERENCES public.cardtypes(id);


ALTER TABLE ONLY public.cardspecialtraits
    ADD CONSTRAINT cardspecialtraits_cardid_fkey FOREIGN KEY (cardid) REFERENCES public.cards(id) ON DELETE CASCADE;


ALTER TABLE ONLY public.cardspecialtraits
    ADD CONSTRAINT cardspecialtraits_specialtraitid_fkey FOREIGN KEY (specialtraitid) REFERENCES public.specialtraits(id);


\unrestrict bynV66OKeFnRtUyNL4h9Fm4uvDhZEH9YHNRQNoc8vGLzVCydEgh1INKevzT6JQ5

