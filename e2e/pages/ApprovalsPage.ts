import type { Locator, Page } from '@playwright/test';

export class ApprovalsPage {
  private readonly panel: Locator;

  constructor(private readonly page: Page) {
    this.panel = page.getByRole('tabpanel', { name: 'Approvals' });
  }

  async open() {
    await this.page.getByRole('tab', { name: 'Approvals' }).click();
  }

  /** The alert under the pending list where refused decisions are explained. */
  get error(): Locator {
    return this.panel.getByRole('alert');
  }

  pendingItem(invoiceId: string): Locator {
    return this.panel.getByRole('listitem').filter({ hasText: invoiceId });
  }

  async approve(invoiceId: string) {
    await this.panel.getByRole('button', { name: `Approve invoice ${invoiceId}`, exact: true }).click();
  }
}
