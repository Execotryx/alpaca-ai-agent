using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AlpacaAgent.Persistence.Migrations;

[Migration("20260829180000_InitialPhase3")]
[DbContext(typeof(WorkflowDbContext))]
public sealed class InitialPhase3 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(Sql);
    protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("Destructive Phase 3 downgrade is intentionally unsupported.");

    private const string Sql = """
CREATE TABLE workflow_cycles (
 cycle_id uuid PRIMARY KEY, schedule_key text NOT NULL UNIQUE, template text NOT NULL, symbol text NOT NULL, strategy text,
 status text NOT NULL CHECK (status IN ('RUNNING','COMPLETED','NO_ACTION','SAFELY_DEFERRED','FAILED_AND_ESCALATED')),
 deadline timestamptz NOT NULL, budgets jsonb NOT NULL DEFAULT '{}'::jsonb, versions jsonb NOT NULL DEFAULT '{}'::jsonb,
 terminal_reason text, created_at timestamptz NOT NULL DEFAULT clock_timestamp());
CREATE TABLE workflow_steps (
 cycle_id uuid NOT NULL REFERENCES workflow_cycles(cycle_id), step_id integer NOT NULL CHECK (step_id BETWEEN 1 AND 18),
 status text NOT NULL CHECK (status IN ('PENDING','RUNNING','COMPLETED','SKIPPED','SAFELY_DEFERRED','FAILED')),
 routing_reason text NOT NULL, due_at timestamptz, input_references jsonb NOT NULL DEFAULT '[]'::jsonb,
 output_references jsonb NOT NULL DEFAULT '[]'::jsonb, lease_owner text, lease_expires_at timestamptz,
 fencing_token bigint NOT NULL DEFAULT 0 CHECK (fencing_token >= 0), attempt_number integer NOT NULL DEFAULT 0 CHECK (attempt_number >= 0),
 PRIMARY KEY (cycle_id, step_id), CHECK ((status = 'RUNNING') = (lease_owner IS NOT NULL AND lease_expires_at IS NOT NULL)));
CREATE INDEX ix_workflow_steps_due ON workflow_steps (due_at, cycle_id, step_id) WHERE status = 'PENDING';
CREATE TABLE workflow_step_attempts (
 attempt_id uuid PRIMARY KEY, cycle_id uuid NOT NULL, step_id integer NOT NULL, attempt_number integer NOT NULL CHECK (attempt_number > 0),
 owner text NOT NULL, fencing_token bigint NOT NULL, database_claim_time timestamptz NOT NULL, input_references jsonb NOT NULL,
 component_versions jsonb NOT NULL, UNIQUE (cycle_id, step_id, attempt_number),
 FOREIGN KEY (cycle_id, step_id) REFERENCES workflow_steps(cycle_id, step_id));
CREATE TABLE workflow_step_attempt_results (
 result_id uuid PRIMARY KEY, attempt_id uuid NOT NULL REFERENCES workflow_step_attempts(attempt_id), observed_at timestamptz NOT NULL,
 authoritative boolean NOT NULL, outcome text NOT NULL, classification text NOT NULL, next_due_at timestamptz,
 output_references jsonb NOT NULL DEFAULT '[]'::jsonb, diagnostic_reference text);
CREATE UNIQUE INDEX ux_step_attempt_authoritative_result ON workflow_step_attempt_results(attempt_id) WHERE authoritative;
CREATE TABLE workflow_checkpoints (
 checkpoint_id uuid PRIMARY KEY, cycle_id uuid NOT NULL REFERENCES workflow_cycles(cycle_id), step_id integer NOT NULL,
 content_hash text NOT NULL, contract_version text NOT NULL, payload_reference text NOT NULL, created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
 UNIQUE(cycle_id, step_id, content_hash, contract_version));
CREATE TABLE outbox_messages (
 outbox_id uuid PRIMARY KEY, deduplication_key text NOT NULL UNIQUE, message_type text NOT NULL, message_version text NOT NULL,
 cycle_id uuid REFERENCES workflow_cycles(cycle_id), intent_id uuid, payload jsonb NOT NULL, status text NOT NULL DEFAULT 'PENDING',
 due_at timestamptz NOT NULL, attempt_count integer NOT NULL DEFAULT 0 CHECK (attempt_count >= 0), maximum_attempts integer NOT NULL CHECK (maximum_attempts > 0),
 lease_owner text, fencing_token bigint NOT NULL DEFAULT 0, lease_expires_at timestamptz, created_at timestamptz NOT NULL DEFAULT clock_timestamp());
CREATE INDEX ix_outbox_due ON outbox_messages(due_at, outbox_id) WHERE status = 'PENDING';
CREATE TABLE outbox_dispatch_attempts (attempt_id uuid PRIMARY KEY, outbox_id uuid NOT NULL REFERENCES outbox_messages(outbox_id), attempt_number integer NOT NULL, owner text NOT NULL, fencing_token bigint NOT NULL, database_claim_time timestamptz NOT NULL, UNIQUE(outbox_id,attempt_number));
CREATE TABLE outbox_dispatch_attempt_results (result_id uuid PRIMARY KEY, attempt_id uuid NOT NULL REFERENCES outbox_dispatch_attempts(attempt_id), observed_at timestamptz NOT NULL, authoritative boolean NOT NULL, outcome text NOT NULL, classification text NOT NULL);
CREATE UNIQUE INDEX ux_outbox_attempt_authoritative_result ON outbox_dispatch_attempt_results(attempt_id) WHERE authoritative;
CREATE TABLE order_intents (
 intent_id uuid PRIMARY KEY, intent_key text NOT NULL UNIQUE, client_order_id text NOT NULL UNIQUE, cycle_id uuid NOT NULL REFERENCES workflow_cycles(cycle_id),
 executable_payload jsonb NOT NULL, executable_hash text NOT NULL, status text NOT NULL, created_at timestamptz NOT NULL DEFAULT clock_timestamp());
ALTER TABLE outbox_messages ADD CONSTRAINT fk_outbox_intent FOREIGN KEY(intent_id) REFERENCES order_intents(intent_id);
CREATE TABLE share_reservations (
 reservation_id uuid PRIMARY KEY, cycle_id uuid NOT NULL REFERENCES workflow_cycles(cycle_id), intent_id uuid NOT NULL REFERENCES order_intents(intent_id),
 holding_id text NOT NULL, share_block integer NOT NULL CHECK (share_block >= 0), quantity numeric NOT NULL CHECK (quantity > 0), active boolean NOT NULL,
 UNIQUE(holding_id, share_block, active));
CREATE TABLE agent_attempts (attempt_id uuid PRIMARY KEY, cycle_id uuid NOT NULL REFERENCES workflow_cycles(cycle_id), payload jsonb NOT NULL, started_at timestamptz NOT NULL);
CREATE TABLE agent_attempt_results (result_id uuid PRIMARY KEY, attempt_id uuid NOT NULL REFERENCES agent_attempts(attempt_id), authoritative boolean NOT NULL, payload jsonb NOT NULL, observed_at timestamptz NOT NULL);
CREATE UNIQUE INDEX ux_agent_attempt_authoritative_result ON agent_attempt_results(attempt_id) WHERE authoritative;
CREATE TABLE broker_operations (operation_id uuid PRIMARY KEY, intent_id uuid REFERENCES order_intents(intent_id), outbox_id uuid REFERENCES outbox_messages(outbox_id), payload jsonb NOT NULL, started_at timestamptz NOT NULL);
CREATE TABLE broker_observations (observation_id uuid PRIMARY KEY, operation_id uuid REFERENCES broker_operations(operation_id), intent_id uuid REFERENCES order_intents(intent_id), payload jsonb NOT NULL, observed_at timestamptz NOT NULL);
CREATE TABLE domain_events (event_id uuid PRIMARY KEY, correlation_id text NOT NULL, cycle_id uuid REFERENCES workflow_cycles(cycle_id), event_type text NOT NULL, contract_version text NOT NULL, payload jsonb NOT NULL, occurred_at timestamptz NOT NULL);
CREATE TABLE workflow_snapshots (snapshot_id uuid PRIMARY KEY, cycle_id uuid NOT NULL REFERENCES workflow_cycles(cycle_id), snapshot_type text NOT NULL, contract_version text NOT NULL, content_hash text NOT NULL, payload jsonb NOT NULL, created_at timestamptz NOT NULL, UNIQUE(snapshot_type, content_hash, contract_version));
CREATE TABLE lifecycle_obligations (obligation_id uuid PRIMARY KEY, cycle_id uuid NOT NULL REFERENCES workflow_cycles(cycle_id), symbol text NOT NULL, contract_id text NOT NULL, open_quantity integer NOT NULL CHECK(open_quantity >= 0), state text NOT NULL, due_at timestamptz NOT NULL);
CREATE TABLE durable_review_schedules (schedule_id uuid PRIMARY KEY, obligation_id uuid NOT NULL REFERENCES lifecycle_obligations(obligation_id), due_at timestamptz NOT NULL, schedule_key text NOT NULL UNIQUE);
CREATE TABLE control_states (control_id uuid PRIMARY KEY, scope_type text NOT NULL CHECK(scope_type IN ('GLOBAL_PAUSE','SYMBOL_PAUSE','STRATEGY_PAUSE','KILL_SWITCH')), scope_key text NOT NULL, active boolean NOT NULL, reason text NOT NULL, actor text NOT NULL, changed_at timestamptz NOT NULL DEFAULT clock_timestamp(), UNIQUE(scope_type, scope_key));
CREATE TABLE control_state_events (event_id uuid PRIMARY KEY, control_id uuid NOT NULL REFERENCES control_states(control_id), prior_value boolean, new_value boolean NOT NULL, actor text NOT NULL, source text NOT NULL, reason text NOT NULL, changed_at timestamptz NOT NULL DEFAULT clock_timestamp());
CREATE OR REPLACE FUNCTION guard_terminal_cycle() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF OLD.status IN ('COMPLETED','NO_ACTION','SAFELY_DEFERRED','FAILED_AND_ESCALATED') AND NEW IS DISTINCT FROM OLD THEN RAISE EXCEPTION 'terminal workflow cycle is immutable'; END IF; RETURN NEW; END $$;
CREATE TRIGGER workflow_cycles_terminal_guard BEFORE UPDATE ON workflow_cycles FOR EACH ROW EXECUTE FUNCTION guard_terminal_cycle();
CREATE OR REPLACE FUNCTION reject_evidence_mutation() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'append-only table % cannot be mutated', TG_TABLE_NAME; END $$;
DO $$ DECLARE t text; BEGIN FOREACH t IN ARRAY ARRAY['workflow_step_attempts','workflow_step_attempt_results','workflow_checkpoints','outbox_dispatch_attempts','outbox_dispatch_attempt_results','agent_attempts','agent_attempt_results','broker_operations','broker_observations','domain_events','workflow_snapshots','control_state_events'] LOOP EXECUTE format('CREATE TRIGGER %I_append_only BEFORE UPDATE OR DELETE ON %I FOR EACH ROW EXECUTE FUNCTION reject_evidence_mutation()', t, t); END LOOP; END $$;
""";
}
