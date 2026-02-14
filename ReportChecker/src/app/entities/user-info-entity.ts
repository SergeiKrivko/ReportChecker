export interface UserInfoEntity {
  id: string;
  accounts: AccountInfoEntity[];
}

export interface AccountInfoEntity {
  provider: string;
  id: string;
  name: string | undefined;
  email: string | undefined;
  avatarUrl: string | undefined;
}
