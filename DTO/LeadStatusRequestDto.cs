using System;
using System.ComponentModel.DataAnnotations;

namespace YourNamespace.Dtos
{
    /// <summary>
    /// Объект с информацией о передаваемом статусе лида
    /// </summary>
    public class LeadStatusRequestDto
    {
        /// <summary>
        /// Идентификатор лида (uuid) в формате ########-####-####-####-############
        /// </summary>
        public string lead_id { get; set; }

        /// <summary>
        /// Строка исходящего статуса, заданная в конфигурации точки интеграции для типа лида
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// Номер рабочего листа
        /// </summary>
        [MaxLength(100)]
        public string client_dms_id { get; set; }

        /// <summary>
        /// Имя пользователя, действия которого повлекли изменения статуса
        /// </summary>
        [MaxLength(256)]
        public string responsible_user { get; set; }

        /// <summary>
        /// Комментарий к исходящему статусу отказа
        /// </summary>
        [MaxLength(500)]
        public string refuse_reason_comment { get; set; }

        /// <summary>
        /// Исходящий идентификатор причины отказа (если статус - "отказ")
        /// </summary>
        public int? dealer_refuse_reason_id { get; set; }

        /// <summary>
        /// Планируемая дата визита
        /// </summary>
        public DateTime? dealer_visit_planned_date { get; set; }

        /// <summary>
        /// Планируемая дата перезвона
        /// </summary>
        public DateTime? dealer_recall_date { get; set; }

        /// <summary>
        /// Комментарий к устанавливаемому статусу. Если комментарий не пустой, то он будет сохранён.
        /// </summary>
        [MaxLength(1000)]
        public string status_comment { get; set; }
    }
}
