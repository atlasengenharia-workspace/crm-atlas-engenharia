namespace CrmAtlas.ApplicationCore.Enums;

public enum AcompanhamentoServicoTipo { AVCB, CLCB, OBRAS, PROCESSOS_ADM }
public enum GoogleIntegrationAction { CONNECT, DISCONNECT, TEST, SYNC }
public enum GoogleIntegrationStatus { CONNECTED, DISCONNECTED, ERROR, PENDING }
public enum LancamentoOrigem { MANUAL, IMPORT_INTER, IMPORT_ASAAS, IMPORT_ATLAS }
public enum LancamentoStatus { PREVISTO, A_PAGAR, PAGO, A_CONFIRMAR }
public enum LancamentoTipo { ENTRADA, SAIDA }
public enum NotificationCategory { FINANCEIRA, TECNICA }
public enum NotificationRuleType { PARCELA_A_VENCER, SERVICO_PARADO }
public enum NotificationServiceType { AVCB, CLCB, OBRAS, PROCESSOS_ADM }
public enum PrestadorPagamentoDataTipo { DATA, A_DEFINIR, TERMINO_SERVICO }
public enum SituacaoAvcb { PENDENTE, EM_ANDAMENTO, CONCLUIDO, CANCELADO }
public enum SituacaoClcb { PENDENTE, EM_ANDAMENTO, CONCLUIDO, CANCELADO }
public enum SituacaoObra { PENDENTE, EM_ANDAMENTO, CONCLUIDO, CANCELADO }
public enum SituacaoProcesso { PENDENTE, EM_ANDAMENTO, CONCLUIDO, CANCELADO }
public enum UserRole { ADMIN, USER }
public enum WhatsAppMetaIntegrationAction { CONNECT, DISCONNECT, TEST, SYNC }
public enum WhatsAppMetaIntegrationStatus { CONNECTED, DISCONNECTED, ERROR, PENDING }
