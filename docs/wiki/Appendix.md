# Appendix: Project Documents

**English**

Every document under `docs/` of the repository. Links point to the `main` branch.

## Reference pages in this wiki

- [Workflows](Workflows): every workflow, composite action and GitHub setting, with why and its alternatives.
- [Containers and Images](Containers-and-Images): every container, where its image comes from and how it is locked down.
- [Security Controls](Security-Controls): the security controls layer by layer, mapped to CIS, SAMM and the OWASP Top 10.
- [Alerts](Alerts): every alert rule, its threshold, route and action, and the service level objectives.
- [Technology Choices](Technology-Choices): why each product was chosen over its alternatives, with the community's reasons, and the CI/CD pipeline and the backup step by step.

## Business and planning

- [Business Case](https://github.com/NF-Hotel/NFHotel/blob/main/docs/business-case.md)
- [Stakeholder Analysis](https://github.com/NF-Hotel/NFHotel/blob/main/docs/stakeholder-analysis.md)
- [Project Plan](https://github.com/NF-Hotel/NFHotel/blob/main/docs/project-plan.md)
- [Artifact Registry](https://github.com/NF-Hotel/NFHotel/blob/main/docs/artifact-registry.md)

## Requirements and domain

- [User Stories](https://github.com/NF-Hotel/NFHotel/blob/main/docs/user-stories.md)
- [Use Case Diagram](https://github.com/NF-Hotel/NFHotel/blob/main/docs/use-case-diagram.md)
- [Domain Dictionary](https://github.com/NF-Hotel/NFHotel/blob/main/docs/dictionary.md)
- [Domain Model (overview)](https://github.com/NF-Hotel/NFHotel/blob/main/docs/dm.md)
- [Domain Model DM-004](https://github.com/NF-Hotel/NFHotel/blob/main/docs/domain-model.md)
- [Design Class Diagram (DCD)](https://github.com/NF-Hotel/NFHotel/blob/main/docs/dcd.md)

### Use cases

- [Domain Model DM-001](https://github.com/NF-Hotel/NFHotel/blob/main/docs/use-cases/uc-001/dm.md)
- [UC-001 Manage Bookings](https://github.com/NF-Hotel/NFHotel/blob/main/docs/use-cases/uc-001/uc.md)
- [Domain Model DM-002](https://github.com/NF-Hotel/NFHotel/blob/main/docs/use-cases/uc-002/dm.md)
- [UC-002 Manage Rooms](https://github.com/NF-Hotel/NFHotel/blob/main/docs/use-cases/uc-002/uc.md)
- [Domain Model DM-003](https://github.com/NF-Hotel/NFHotel/blob/main/docs/use-cases/uc-003/dm.md)
- [UC-003 Manage Guests](https://github.com/NF-Hotel/NFHotel/blob/main/docs/use-cases/uc-003/uc.md)
- [Domain Model DM-005](https://github.com/NF-Hotel/NFHotel/blob/main/docs/use-cases/uc-004/dm.md)
- [UC-004 Sign In To The Back Office](https://github.com/NF-Hotel/NFHotel/blob/main/docs/use-cases/uc-004/uc.md)
- [Wireframes of UC-004](https://github.com/NF-Hotel/NFHotel/blob/main/docs/use-cases/uc-004/wireframe.md)

## Architecture decisions

- [ADR-0001 Clean architecture structure of NFHotel](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0001-clean-architecture-structure.md)
- [ADR-0002 Validation attributes on the entities](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0002-validation-attributes-on-entities.md)
- [ADR-0003 Services run in a virtual machine on Proxmox VE](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0003-proxmox-vm-for-services.md)
- [ADR-0004 Only an internal API reaches the database](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0004-database-api.md)
- [ADR-0005 Staff sign-in and roles for the back office](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0005-backoffice-login.md)
- [ADR-0006 TLS at the public port](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0006-tls.md)

## Milestones

- [MIL-001 Proxmox integration test environment](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-001-proxmox-test-env.md)
- [MIL-002 Nginx proxy and web service in Docker Compose](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-002-nginx-proxy-and-web-compose.md)
- [MIL-003 Throwaway Proxmox container for tests](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-003-throwaway-proxmox.md)
- [MIL-004 Market analysis service on a Proxmox VM behind nginx](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-004-market-analysis-service.md)
- [MIL-005 Clean architecture structure for NFHotel](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-005-clean-architecture.md)
- [MIL-006 Legacy project documents in English](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-006-english-legacy-documents.md)
- [MIL-007 Domain Model of the hotel back office](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-007-domain-model.md)
- [MIL-008 Domain rules live with the data they guard](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-008-domain-rules-in-entities.md)
- [MIL-009 Validation attributes on the entities](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-009-validation-attributes.md)
- [MIL-010 Source comments in English](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-010-english-source-comments.md)
- [MIL-011 Move the tests from xUnit v2 to xUnit v3](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-011-xunit-v3.md)
- [MIL-012 Tests follow the test rules of the coding conventions](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-012-test-kinds.md)
- [MIL-013 Booking guards: inverted overview period and check-in to a blocked room](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-013-booking-guards.md)
- [MIL-014 Async where it gives meaning, layer by layer](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-014-async-where-it-matters.md)
- [MIL-015 A database API: only the API reaches the database](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-015-database-api.md)
- [MIL-016 Login and roles for the back office](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-016-backoffice-login.md)
- [MIL-017 TLS at the public port](https://github.com/NF-Hotel/NFHotel/blob/main/docs/milestones/mil-017-tls.md)

## Quality assurance

- [Traceability Matrix](https://github.com/NF-Hotel/NFHotel/blob/main/docs/sqa/traceability-matrix.md)
- [Review records RC-001 to RC-082 (folder)](https://github.com/NF-Hotel/NFHotel/tree/main/docs/sqa/reviews)

## Legacy refactor documents

- [Access-Control-Matrix.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/Access-Control-Matrix.md)
- [Analysis.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/Analysis.md)
- [Architecture-Diagrams.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/Architecture-Diagrams.md)
- [Architecture-Domain.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/Architecture-Domain.md)
- [Architecture-Layers.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/Architecture-Layers.md)
- [Architecture.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/Architecture.md)
- [BusinessRules.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/BusinessRules.md)
- [Decisions.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/Decisions.md)
- [Encryption-Key-Management.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/Encryption-Key-Management.md)
- [Guest-Anonymization-Strategy.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/Guest-Anonymization-Strategy.md)
- [Models.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/Models.md)
- [OpenQuestions-Tracks.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/OpenQuestions-Tracks.md)
- [OpenQuestions.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/OpenQuestions.md)
- [Phase1-Scope.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/Phase1-Scope.md)
- [README-backend.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/README-backend.md)
- [Review-DCD-Data.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/Review-DCD-Data.md)
- [Testplan.md](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/Testplan.md)
- [frontend-guild-presentation.html](https://github.com/NF-Hotel/NFHotel/blob/main/docs/refactor/frontend-guild-presentation.html)
- [Diagrams (folder)](https://github.com/NF-Hotel/NFHotel/tree/main/docs/refactor/diagrams)
