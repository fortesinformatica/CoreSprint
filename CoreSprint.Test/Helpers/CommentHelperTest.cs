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

            Assert.AreEqual(new DateTime(2015, 7, 7, 0, 0, 0), date);
        }

        [Test]
        public void PossoUtilizarADataDoComentarioQuandoNaoInformadoNoComentarioDeTrabalho()
        {
            var comment = new CommentCardAction
            {
                Date = new DateTime(2015, 7, 8, 3, 0, 0),
                Data = new CommentCardAction.ActionData
                {
                    Text = "> trabalhado 1 hora"
                }
            };

            var date = _commentHelper.GetDateInComment(comment);
            Assert.AreEqual(new DateTime(2015, 7, 8), date);
        }

        [Test]
        public void PossoUtilizarADataComAHoraEMinutoAlteradosPorUmComentarioDeInicioDeTrabalho()
        {
            var comment = new CommentCardAction
            {
                Date = new DateTime(2015, 7, 8, 16, 15, 0),
                Data = new CommentCardAction.ActionData
                {
                    Text = "> inicia em 13:30"
                }
            };

            var date = _commentHelper.GetDateInComment(comment);
            Assert.AreEqual(new DateTime(2015, 7, 8, 13, 30, 00), date);
        }

        [Test]
        public void PossoUtilizarADataComAHoraEMinutoAlteradosPorUmComentarioDePausaDeTrabalho()
        {
            var comment = new CommentCardAction
            {
                Date = new DateTime(2015, 7, 8, 16, 15, 0),
                Data = new CommentCardAction.ActionData
                {
                    Text = "> pausa em 13:30"
                }
            };

            var date = _commentHelper.GetDateInComment(comment);
            Assert.AreEqual(new DateTime(2015, 7, 8, 13, 30, 00), date);
        }
    }
}
