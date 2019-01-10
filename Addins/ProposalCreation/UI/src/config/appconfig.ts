export class AppConfig {
    private static ApplicationId: string = "<APPLICATION_ID>";
    static get applicationId(): string { return AppConfig.ApplicationId; }
    static get accessTokenKey(): string { return "webapiAccessToken"; }
    static get title(): string { return "Proposal Creation"; }
}