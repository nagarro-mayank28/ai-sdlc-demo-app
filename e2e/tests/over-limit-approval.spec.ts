import { expect, test } from '@playwright/test';
import { ApprovalsPage } from '../pages/ApprovalsPage';
import { InvoicesPage } from '../pages/InvoicesPage';
import { demoUsers, LoginPage } from '../pages/LoginPage';

// Spec 001, User Story 3: a refused over-limit approval is explained and changes nothing.
test('a manager approving an invoice over their limit sees why, and the invoice stays pending', async ({ page }) => {
  const customer = `E2E Over Limit ${Date.now()}`;

  await new LoginPage(page).signIn(demoUsers.manager);
  const invoices = new InvoicesPage(page);
  const invoiceId = await invoices.createAndSubmit(customer, '75000');

  const approvals = new ApprovalsPage(page);
  await approvals.open();

  const approveResponse = page.waitForResponse((response) =>
    response.url().endsWith(`/api/approvals/${invoiceId}/approve`));
  await approvals.approve(invoiceId);
  expect((await approveResponse).status()).toBe(422);

  await expect(approvals.error).toBeVisible();
  await expect(approvals.error).toContainText(`Invoice ${invoiceId} is 75,000.00`);
  await expect(approvals.error).toContainText('over the Manager approval limit of 50,000.00');
  await expect(approvals.error).toContainText('A Director must approve it');

  // Nothing changed: still in the pending list, and still Pending Approval in the invoice table.
  await expect(approvals.pendingItem(invoiceId)).toBeVisible();
  await invoices.open();
  await expect(invoices.statusCell(invoiceId)).toHaveText('Pending Approval');
});
