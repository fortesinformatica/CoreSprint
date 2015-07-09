using System;
using CoreSprint.Helpers;
using NUnit.Framework;
using TrelloNet;

namespace CoreSprint.Test.Helpers
{
    [TestFixture]
    public class CommentHelperTest
    {
        private ICommentHelper _commentHelper;

        [SetUp]
        public void Cenario()
        {
            _commentHelper = new CommentHelper();
        }

        [Test]
        public void PossoReconhecerUmaDataNumComentarioDeTrabalho()
        {
            var comment = new CommentCardAction
            {
                Data = new CommentCardAction.ActionData
                {
                    Text = "> trabalhado 1 hora em 07/07/2015\r\n\r\n- testando isto aqui"
                }
            };

            var date = _commentHelper.GetDateInComment(comment);

            Assert.AreEqual(new DateTime(2015, 7, 7), date);
        }

        [Test]
        public void PossoUtilizarADataDoComentarioQuandoNaoInformadoNoComentarioDeTrabalho()
        {
            var comment = new CommentCardAction
            {
                Date = new DateTime(2015, 7, 8),
                Data = new CommentCardAction.ActionData
                {
                    Text = "> trabalhado 1 hora"
                }
            };

            var date = _commentHelper.GetDateInComment(comment);
            Assert.AreEqual(new DateTime(2015, 7, 8), date);
        }
    }
}
