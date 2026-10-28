import { expect, type Locator, type Page } from '@playwright/test';

export class InvoicesPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.getByRole('tab', { name: 'Invoices' }).click();
  }

  rowFor(text: string): Locator {
    return this.page
      .getByRole('table', { name: 'All invoices' })
      .getByRole('row')
      .filter({ has: this.page.getByRole('cell', { name: text, exact: true }) });
  }

  statusCell(invoiceId: string): Locator {
    return this.rowFor(invoiceId).getByRole('cell').nth(3);
  }

  /** Creates a draft through the form, submits it for approval and returns its generated id. */
  async createAndSubmit(customerName: string, amount: string): Promise<string> {
    await this.open();
    await this.page.getByLabel('Customer name').fill(customerName);
    await this.page.getByLabel('Amount', { exact: true }).fill(amount);
    await this.page.getByRole('button', { name: 'Create invoice' }).click();

    const row = this.rowFor(customerName);
    await expect(row).toBeVisible();
    const invoiceId = (await row.getByRole('cell').first().innerText()).trim();

    await row.getByRole('button', { name: `Submit invoice ${invoiceId} for approval` }).click();
    await expect(this.statusCell(invoiceId)).toHaveText('Pending Approval');
    return invoiceId;
  }
}
