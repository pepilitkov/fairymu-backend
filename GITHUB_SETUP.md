# Upload to GitHub

Препоръчително е backend-ът да бъде в отделно repository, например:

`fairymu-backend`

Качи **съдържанието** на този folder в root на repository-то, така че да изглежда:

```text
fairymu-backend/
├── .github/
│   └── workflows/
│       └── backend-ci.yml
├── Contracts/
├── Models/
├── Services/
├── scripts/
│   └── smoke-test.sh
├── FairyMU.Api.csproj
├── Program.cs
├── appsettings.json
├── openapi.yaml
└── README.md
```

След commit към `main`:
1. отвори **Actions**;
2. избери **FairyMU Backend CI**;
3. отвори run-а;
4. провери `Build Release`;
5. провери `Run API smoke tests`;
6. при успех свали `fairymu-backend-...` artifact.

Не качвай:
- реални database passwords;
- API secrets;
- private keys;
- `appsettings.Production.json` с credentials.
