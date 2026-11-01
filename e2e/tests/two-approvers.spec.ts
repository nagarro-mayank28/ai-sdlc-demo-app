import { expect, test } from '@playwright/test';
import { ApprovalsPage } from '../pages/ApprovalsPage';
import { InvoicesPage } from '../pages/InvoicesPage';
import { demoUsers, LoginPage } from '../pages/LoginPage';

// Spec 001, User Story 2 and User Story 3 scenario 2: above 500,000 two different directors must approve.
test('an invoice over 500,000 needs a second, different director', async ({ page }) => {
  const customer = `E2E Two Approvers ${Date.now()}`;
  const login = new LoginPage(page);
  const invoices = new InvoicesPage(page);
  const approvals = new ApprovalsPage(page);

  await login.signIn(demoUsers.director);
  const invoiceId = await invoices.createAndSubmit(customer, '750000');

  await approvals.open();
  await approvals.approve(invoiceId);
  await expect(approvals.pendingItem(invoiceId)).toContainText('Awaiting second approval (first: demo.director)');

  // Same director again: refused, still awaiting.
  await approvals.approve(invoiceId);
  await expect(approvals.error).toContainText('second approval must come from a different person');
  await expect(approvals.pendingItem(invoiceId)).toContainText('Awaiting second approval');

  await login.signOut();
  await login.signIn(demoUsers.secondDirector);
  await approvals.open();
  await approvals.approve(invoiceId);
  await expect(approvals.pendingItem(invoiceId)).toHaveCount(0);

  await invoices.open();
  await expect(invoices.statusCell(invoiceId)).toHaveText('Approved');
});
