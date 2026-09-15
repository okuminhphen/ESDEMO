export type AuthUser = {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
};

export type TokenResponse = {
  accessToken: string;
  tokenType: "Bearer";
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: AuthUser;
};

export type LoginPayload = {
  email: string;
  password: string;
};

export type RegisterPayload = LoginPayload & {
  displayName: string;
  confirmPassword: string;
};
