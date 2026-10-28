import { expect, type Page } from '@playwright/test';

/** Demo accounts from appsettings.json. Local, in-memory demo only; not real credentials. */
export const demoUsers = {
  manager: { username: 'demo.manager', password: 'ChangeMe123!' },
  director: { username: 'demo.director', password: 'ChangeMe123!' },
  secondDirector: { username: 'demo.director2', password: 'ChangeMe123!' },
} as const;

export class LoginPage {
  constructor(private readonly page: Page) {}

  async signIn(user: { username: string; password: string }) {
    await this.page.goto('/');
    await this.page.getByLabel('Username').fill(user.username);
    await this.page.getByLabel('Password').fill(user.password);
    await this.page.getByRole('button', { name: 'Sign in' }).click();
    await expect(this.page.getByRole('button', { name: 'Sign out' })).toBeVisible();
  }
}
