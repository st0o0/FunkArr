# Changelog

## [0.1.1](https://github.com/st0o0/FunkArr/compare/v0.1.0...v0.1.1) (2026-09-04)


### Features

* add community ruleset data ([e22cb0a](https://github.com/st0o0/FunkArr/commit/e22cb0a1ac361edabb5c2a6783e90c0b645539c2))
* add core FunkArr application ([72d891a](https://github.com/st0o0/FunkArr/commit/72d891abdf9100a24d25f6c15451fdd0634d72c2))
* add deregistration, ID-based resolution, and query to RuleSetResolver ([076488a](https://github.com/st0o0/FunkArr/commit/076488a13563229ed4c515bf017654b84c335540))
* add download domain with FFmpeg pipeline ([78867a2](https://github.com/st0o0/FunkArr/commit/78867a28e6ecbd81167de7b9500bf099b2995ae0))
* add DownloadOptions, ReleaseTitleBuilder, and MetadataSpec ([5221a60](https://github.com/st0o0/FunkArr/commit/5221a60886b2fa031d6be1295bc69c3258359dde))
* add internal download API and update ArrApi for new download domain ([d07e8a1](https://github.com/st0o0/FunkArr/commit/d07e8a18f4cfa243bdbbf05f485de27e53f45d4a))
* add marker interfaces for typed message dispatch ([4197fbf](https://github.com/st0o0/FunkArr/commit/4197fbf42574f91398cf8d44174e834f0736e998))
* add match history persistence and scoring trace recording ([ac5f58e](https://github.com/st0o0/FunkArr/commit/ac5f58eca99a54d4d87058675e082f2c8efde14a))
* add media ID attributes to Newznab RSS and resolve options via DI ([b225e83](https://github.com/st0o0/FunkArr/commit/b225e83554946a5376c5acc38a1269ade0886448))
* add media ID extraction and merge to RuleSetMerger ([9c9f825](https://github.com/st0o0/FunkArr/commit/9c9f8253191e07c6786a961a1a4ac34431e6014a))
* add messages for ID-based search, deregistration, and query ([2996b55](https://github.com/st0o0/FunkArr/commit/2996b554c73a57ab4340459eefe5413e34092a35))
* add MetadataResolver domain with TMDB and TVDB resolution ([dea83f0](https://github.com/st0o0/FunkArr/commit/dea83f0fa3234afc0470cd5e7cca717f5960f602))
* add multi-project solution with domain isolation ([f030caa](https://github.com/st0o0/FunkArr/commit/f030caad682bf09b64520b9e838f532a8d367e37))
* add OpenAPI spec with generated contracts and update controllers ([a0bace9](https://github.com/st0o0/FunkArr/commit/a0bace9f92bbee125c0aad4af0c93aee0c356920))
* add RemoveMatchingConfig and inject options via DI in MatchMagic ([7e30a43](https://github.com/st0o0/FunkArr/commit/7e30a43c92713fd9a62e9587e66613fdb6001987))
* add RuleSet API, Setup API, and OpenAPI support ([0483068](https://github.com/st0o0/FunkArr/commit/0483068f32480361be981b014b5f759a0667b7cc))
* add RuleSet domain messages ([c44e7c6](https://github.com/st0o0/FunkArr/commit/c44e7c6dd489166fc05dec1095424ffbca13dd20))
* add RuleSet domain with resolver, merger, and sharded workers ([a4ecef8](https://github.com/st0o0/FunkArr/commit/a4ecef85c392bfaff9e340f795af7b13e778e9e8))
* add RuleSet write and test APIs with improved merger ([ab0f2c3](https://github.com/st0o0/FunkArr/commit/ab0f2c3691665b3d6317578a036478ae5c3e9085))
* add RuleSetManager filewatcher, updater, and detail query ([469ff6d](https://github.com/st0o0/FunkArr/commit/469ff6d1a0326d325a795d115bc59a3af76ff006))
* add scoring origin tracking and history messages ([df29c53](https://github.com/st0o0/FunkArr/commit/df29c531c479cb221d9630839bdf854b0982f471))
* add search pagination with limit/offset through pipeline ([d61737b](https://github.com/st0o0/FunkArr/commit/d61737bfe512de495c769050c701e03182b88e42))
* add setup validation service with arr registration checks ([d911858](https://github.com/st0o0/FunkArr/commit/d911858688f96ed1d0fa967a16493d37d743605c))
* add shared content filter for search result pre-filtering ([ace2755](https://github.com/st0o0/FunkArr/commit/ace275538c28cf8f914e01caebc292b0d23818e8))
* add sidebar layout and design tokens to UI ([9ce4691](https://github.com/st0o0/FunkArr/commit/9ce46910748706ca24eb250c2d5ea93f22bd087e))
* add solution and build configuration ([70b7322](https://github.com/st0o0/FunkArr/commit/70b7322cbddd40d491319893c3071cfb0ee94c75))
* add test infrastructure and tests ([27d30f2](https://github.com/st0o0/FunkArr/commit/27d30f2ff0ff110c744ea2d072ee5349d07ebd66))
* add Vue.js frontend with setup, ruleset, and scoring views ([e227328](https://github.com/st0o0/FunkArr/commit/e227328547d102ce40045b9f66dd3fdc265b2b8c))
* add Vue.js web UI ([b51fe52](https://github.com/st0o0/FunkArr/commit/b51fe52748fccf5ed9e8b60b9756a02097309126))
* API improvements with TypedResults, OpenAPI, and Mediathek proxy ([9c1a7b7](https://github.com/st0o0/FunkArr/commit/9c1a7b7d3cd735a2aad9ff236378e9745ac9243d))
* decompose options into separate config classes ([ce35d48](https://github.com/st0o0/FunkArr/commit/ce35d489772ddd874cfb925134cb635edcdbf806))
* extract download service from download queue actor ([6ce44ab](https://github.com/st0o0/FunkArr/commit/6ce44abed7dec0075506fe8e55587e5cbce8f5a4))
* extract MediathekApiModels and add ID-based search to workers ([53f408d](https://github.com/st0o0/FunkArr/commit/53f408d8448bf815b71e7877b6aaf7fbdb633742))
* extract Newznab search handler and add category support ([ba8bc6f](https://github.com/st0o0/FunkArr/commit/ba8bc6f7b07f4806123d997ddd1a4b0571db20b1))
* generate scene-style release titles in search workers ([dad1b94](https://github.com/st0o0/FunkArr/commit/dad1b94f5d1e33e760d1671e8a32fd136f0dbb40))
* integrate RuleSet resolver into search pipeline ([c1bdc10](https://github.com/st0o0/FunkArr/commit/c1bdc10a2591d0218ef0cf892846113ad169deae))
* rebuild MatchMagic with config-driven scoring and actor pool ([e16681d](https://github.com/st0o0/FunkArr/commit/e16681d17f9e8317b6f6ca5b4dd1aaa464f49ae9))
* redesign ruleset schema with ID-based layering ([82dc912](https://github.com/st0o0/FunkArr/commit/82dc91296f7b36b3db3f0cdde07f922362c31b47))
* replace scoring messages with config-driven matching types ([c8dd339](https://github.com/st0o0/FunkArr/commit/c8dd339e50ff4b4fbe6fa5dabe97e2a5315e3377))
* rewrite download domain with persistent workers and history manager ([8c962e3](https://github.com/st0o0/FunkArr/commit/8c962e3ef600931b80e9fbfada565425a665d6aa))
* RuleSet builder UI with debugger and trace views ([c23ae9b](https://github.com/st0o0/FunkArr/commit/c23ae9bd089597022794988ddb2b8b93bc072cad))
* split search actor into specialized actors ([b83b981](https://github.com/st0o0/FunkArr/commit/b83b981dec66519429f4ef4e962e7fa1af331135))
* UI design refresh with download queue and history views ([da658a6](https://github.com/st0o0/FunkArr/commit/da658a6dc40fb6c7b76f494c46296b7a2277172c))
* UI/UX polish with warm amber palette, collapsible sidebar, transitions, and feedback ([9c9ea25](https://github.com/st0o0/FunkArr/commit/9c9ea2564fab69a41213126217048d17fd4a4696))
* unify search commands and improve search workers ([e5bea03](https://github.com/st0o0/FunkArr/commit/e5bea037f556453ed7624b2ed62e3e8d33e917d9))
* update frontend API paths to versioned /api/v1/ endpoints ([fb9e0ee](https://github.com/st0o0/FunkArr/commit/fb9e0ee6507ba28e1d565fed62d4b31db1da3c63))
* wire download history manager and DownloadOptions in host config ([141c2f9](https://github.com/st0o0/FunkArr/commit/141c2f9f6765e9971c14a1e69eb1758908e8144c))
* wire SABnzbd API to download actor system ([a9d2faa](https://github.com/st0o0/FunkArr/commit/a9d2faac31f0dcc5b0c7bdd628923a56f8fdd7f6))


### Bug Fixes

* resolve CI failures in security, build, and test workflows ([c14ae99](https://github.com/st0o0/FunkArr/commit/c14ae993fe57579d8c3bd12a356c8af0d4c31b4a))


### Documentation

* add OpenSpec specifications and project docs ([7be9221](https://github.com/st0o0/FunkArr/commit/7be9221378d94cd338521e4e0beeaff664b5938d))
* archive completed OpenSpec changes and move config to src/openspec ([635bf68](https://github.com/st0o0/FunkArr/commit/635bf6838367215fae96cb44158406a14a317389))
* update OpenSpec specs and archive architecture redesign changes ([99ae585](https://github.com/st0o0/FunkArr/commit/99ae58590abe8900e19d6734e56bed9458c3ec89))
* update OpenSpec specs and archive controller-migration change ([e0098c5](https://github.com/st0o0/FunkArr/commit/e0098c55ef88ad1cb12944278213c6297fdb2318))
* update OpenSpec specs for redesigned architecture ([0fd101b](https://github.com/st0o0/FunkArr/commit/0fd101b16e0f9ca567591caabe755a3a22bdef53))
* update project documentation and configuration for redesigned architecture ([e5b6b61](https://github.com/st0o0/FunkArr/commit/e5b6b61628c241e7cf509c28a0dc4c9accd442b6))


### Refactoring

* architecture redesign with actor-per-step pipeline and cluster sharding ([ce2651d](https://github.com/st0o0/FunkArr/commit/ce2651dc056c3a2263e3c89d416b07063b8b35d8))
* extract actor state into dedicated files with Apply pattern ([8083bcd](https://github.com/st0o0/FunkArr/commit/8083bcd62839ec3357c0c5fd901c74cbe395cd73))
* extract DataPaths, DataFiles, and FfmpegRunner ([d274be6](https://github.com/st0o0/FunkArr/commit/d274be64df76a5bbe77d97bf52cbef742729b9ef))
* migrate endpoints to MVC controllers with API versioning and Scalar ([c61abe8](https://github.com/st0o0/FunkArr/commit/c61abe8b0ab7c795c12a0a48db34125e7491cb4a))
* move options classes from Host to Core for domain access ([82598ca](https://github.com/st0o0/FunkArr/commit/82598caedecd48afcb0ea30a090e10fa90297da2))
* redesign download messages and persistence events ([a84736f](https://github.com/st0o0/FunkArr/commit/a84736f555131967b7be517b7eed6c9b1433649e))
* rename actor keys to match registration type convention ([f3accc9](https://github.com/st0o0/FunkArr/commit/f3accc9a70663b0028157e6bbb3bf6a7e20fcf44))
* resolve ArrApi endpoint dependencies via DI ([9c491de](https://github.com/st0o0/FunkArr/commit/9c491de5637833eaa5f9d39f33ec08610bafdca7))
* resolve MatchMagic history via ActorRegistry ([fb7910c](https://github.com/st0o0/FunkArr/commit/fb7910c0ee11f2ef9c2e7ef778dd40834973cc03))
* restructure source into domain subfolders with actor naming ([05e5787](https://github.com/st0o0/FunkArr/commit/05e57872e8b861d6999febdbf0fafd5dd11a6688))
* RuleSet state to immutable collections with self-starting scan ([67313d6](https://github.com/st0o0/FunkArr/commit/67313d640ad2d4a6feeca25a5e2ae46bddcae46d))
* update ruleset pipeline and github release client ([d2b5efc](https://github.com/st0o0/FunkArr/commit/d2b5efca199747f7013f8e62cecce0ea00104a9f))


### Dependencies

* bump actions/checkout from 4 to 7 ([e78d95a](https://github.com/st0o0/FunkArr/commit/e78d95a7c138070cd5bf487097233911b3f972dd))
* bump docker/login-action from 3 to 4 ([7f26b9f](https://github.com/st0o0/FunkArr/commit/7f26b9ffdf7bd87b48e24e2251ba23ad76541ad1))
* bump softprops/action-gh-release from 2 to 3 ([f964b10](https://github.com/st0o0/FunkArr/commit/f964b105b1717478b6a1a7a7a5f23d5ae984ca88))
