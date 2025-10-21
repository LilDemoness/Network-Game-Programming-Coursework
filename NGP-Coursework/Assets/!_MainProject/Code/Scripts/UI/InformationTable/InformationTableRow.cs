using TMPro;
using UnityEngine;

namespace UI.Tables
{
    public class InformationTableRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _rowName;
        [SerializeField] private TMP_Text _rowContent;


        public void Show() => this.gameObject.SetActive(true);
        public void Hide() => this.gameObject.SetActive(false);
        public void SetText(string categoryName, string categoryText)
        {
            this._rowName.text = categoryName;
            this._rowContent.text = categoryText;
        }
    }
}