namespace WhiteBalance {
    public partial class ColorTempAdjust {

        #region 固定G值算法
        /// <summary>
        /// 白平衡调整定时器超时函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void FixedGGaimAdjust()
        {
            circle_count++;
            LogHelper.WriteInfoNdc(string.Format("StepNow = {0}, CurRGBValue.RValue = {1}, CurRGBValue.GValue = {2}, " +
                                 "CurRGBValue.BValue = {3}, TimeWatch = {4}｝",
                                 StepNow, CurRGBValue.RValue, CurRGBValue.GValue, CurRGBValue.BValue, AdjustTimeWatch.ElapsedMilliseconds), "FixedGGaimAdjust-"+ circle_count);


#if DEBUG
            Console.WriteLine("WhiBalFixedGGaim_Elapsed StepNow = " + StepNow);
            Console.WriteLine("CurRGBValue.RValue = " + CurRGBValue.RValue);
            Console.WriteLine("CurRGBValue.GValue = " + CurRGBValue.GValue);
            Console.WriteLine("CurRGBValue.BValue = " + CurRGBValue.BValue);
            Console.WriteLine("AdjustTimeWatch.ElapsedMilliseconds = " + AdjustTimeWatch.ElapsedMilliseconds);
#endif
            /* 每个色温超过调整时间即为失败 */
            if (AdjustTimeWatch.ElapsedMilliseconds > AdjustMaxTime) {
                AdjustTimeWatch.Reset();
                AdjustTimeWatch.Stop();
                Trigger_AdjustEvent(this, new AdjustEventArgs(WhiteBalanceAdjustStatus.AdjustTimeOut, GetMultLangStr.GetStr(LanguageInfo.proRunMultLangXmlPath, "StrAdjsutTimeOut", LanguageInfo.language)));
                return;
            }

            switch (StepNow) {
                case AdjustStep.CheckValueOK: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) 
                        {
                            if (GaimIsWithinRange(true) != 0)
                            {
                                Trigger_SendCmdEvent(this, new SendCmdEventArgs(CurRGBValue));
                                return;
                            }
                            else 
                            {
                                if (GetColorTemp())
                                    return;

                                //UpLoadInfoToMes(StepNow);
                                CurRGBValue.RValue -= 1;
                                FirstJudge = true;
                                StepNow = AdjustStep.CheckRPosiDirSatur;
                                DealTvReturnOk();
                                return;
                            }
                        }

                        if (CommStatu.RetOk) {
                            if (GetColorTemp())
                                return;

                            //UpLoadInfoToMes(StepNow);
                            CurRGBValue.RValue -= 1;
                            FirstJudge = true;
                            StepNow = AdjustStep.CheckRPosiDirSatur;
                            DealTvReturnOk();
                        } else
                            DealTvReturnErr();
                    }
                    break;

                /* 设置平均值到电视里 */
                case AdjustStep.SetAverageValue: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {
                            Trigger_SendCmdEvent(this, new SendCmdEventArgs(CurRGBValue));
                            return;
                        }
                        if (CommStatu.RetOk) {
                            CurColorTemp = ColAnalyzer.GetColorTemp();
                            if (IsLvTooLow())
                                return;

                            /* 设置平均值后检测X和Y是否是OK的 */
                            if (CheckIsXAndYOK()) {
                                FirstJudge = true;
                                StepNow = AdjustStep.AdjustSuccess;
                                DealTvReturnOk();
                                break;
                            }
                            FirstJudge = true;
                            //StepNow = CheckIsXOk() ? AdjustStep.AdjustY_B : AdjustStep.AdjustRToTar;
                            StepNow = CheckIsYOk() ? AdjustStep.AdjustRToTar : AdjustStep.AdjustY_B;
                            DealTvReturnOk();
                        } else
                            DealTvReturnErr();
                    }
                    break;

                case AdjustStep.SetSharp648AverageRValue: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {
                            Trigger_SendCmdEvent(this, new SendCmdEventArgs(new RGBValue(CurRGBValue.RValue, TvRGBValue.GValue, TvRGBValue.BValue)));
                            return;
                        }
                        if (CommStatu.RetOk) {
                            StepNow = AdjustStep.SetSharp648AverageGValue;
                            DealTvReturnOk();
                        }
                        else
                            DealTvReturnErr();
                    }
                    break;

                case AdjustStep.SetSharp648AverageGValue: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {
                            Trigger_SendCmdEvent(this, new SendCmdEventArgs(new RGBValue(CurRGBValue.RValue, CurRGBValue.GValue, TvRGBValue.BValue)));
                            return;
                        }
                        if (CommStatu.RetOk) {
                            StepNow = AdjustStep.SetSharp648AverageBValue;
                            DealTvReturnOk();
                        }
                        else
                            DealTvReturnErr();
                    }
                    break;

                case AdjustStep.SetSharp648AverageBValue: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {
                            Trigger_SendCmdEvent(this, new SendCmdEventArgs(new RGBValue(CurRGBValue.RValue, CurRGBValue.GValue, CurRGBValue.BValue)));
                            return;
                        }
                        if (CommStatu.RetOk) {
                            CurColorTemp = ColAnalyzer.GetColorTemp();
                            if (IsLvTooLow())
                                return;

                            /* 设置平均值后检测X和Y是否是OK的 */
                            if (CheckIsXAndYOK()) {
                                StepNow = AdjustStep.AdjustSuccess;
                                FirstJudge = true;
                                DealTvReturnOk();
                                return;
                            }
                            StepNow = CheckIsYOk() ? AdjustStep.AdjustX_R : AdjustStep.AdjustY_B;
                            DealTvReturnOk();
                        }
                        else
                            DealTvReturnErr();
                    }
                    break;

                case AdjustStep.LetXLessThanYSetSharp648RValue: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {
                            Trigger_SendCmdEvent(this, new SendCmdEventArgs(CurRGBValue));
                            return;
                        }
                        if (CommStatu.RetOk) {
                            StepNow = AdjustStep.LetXLessThanYSetSharp648BValue;
                            DealTvReturnOk();
                        }
                        else {
                            if (NeedReduceRGaim)
                                CurRGBValue.RValue += 2;
                            DealTvReturnErr();
                        }

                    }
                    break;

                case AdjustStep.LetXLessThanYSetSharp648BValue: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {
                            Trigger_SendCmdEvent(this, new SendCmdEventArgs(CurRGBValue));
                            return;
                        }
                        if (CommStatu.RetOk) {
                            StepNow = AdjustStep.LetXLessThanY;
                            DealTvReturnOk();
                        }
                        else {
                            if (NeedReduceBGaim)
                                CurRGBValue.BValue += 1;
                            DealTvReturnErr();
                        }

                    }
                    break;

                case AdjustStep.AdjustRToTar: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {
                            /* 如果是因为超时跑进来，证明上一次发送过去的RGB Tv没响应，以防万一，如果之前增加了R值，这里给减掉 */
                            if (CommStatu.RetTimeOut) {
                                ResetFlag();
                                CurRGBValue.RValue += (byte)(CurColorTemp.X > TarColorTemp.X ? StepX : -StepX);
                            }

                            if (FirstJudge)
                                FirstJudge = false;
                            else {
                                if (GetColorTemp())
                                    return;

                                if (CheckIsXOk()) {
                                    StepNow = CheckIsYOk() ? AdjustStep.AdjustSuccess : AdjustStep.AdjustY_B;
                                    FirstJudge = true;
                                    DealTvReturnOk();
                                    break;
                                }
								//R值达到饱和
                                if (CurColorTemp.X < TarColorTemp.X && Math.Abs(PreColorTemp.X - CurColorTemp.X) < StepX * 2) {
                                    R_Status = RGB_STATUS.MAX;
                                    FirstJudge = true;
                                    CurRGBValue.RValue -= StepX;
                                    if (B_Status == RGB_STATUS.MIN)
                                    {
                                        WhiteBalanceAdjustFail(StepNow);
                                        return;
                                    }
                                    else
                                    {
                                        StepNow = AdjustStep.AdjustX_B;
                                        DealTvReturnOk();
                                        break;
                                    }
                                }

                                //R值达到最小
                                if ((CurColorTemp.X > TarColorTemp.X) && ((CurRGBValue.RValue - StepX) < MinRGBGaim.RValue))
                                {
                                    FirstJudge = true;
                                    R_Status = RGB_STATUS.MIN;
                                    if (B_Status == RGB_STATUS.MAX)
                                    {
                                        WhiteBalanceAdjustFail(StepNow);
                                        return;
                                    }
                                    else
                                    {
                                        StepNow = AdjustStep.AdjustX_B;
                                        DealTvReturnOk();
                                        break;
                                    }
                                }
                                Boolean AdjustValid = (CurColorTemp.X > TarColorTemp.X) ? (PreColorTemp.X > CurColorTemp.X) : (PreColorTemp.X < CurColorTemp.X);
                                if (AdjustValid == false)
                                {
                                    adjust_invalid_count++;
									LogHelper.WriteInfoNdc(StepNow + "AdjustValid", adjust_invalid_count.ToString());
                                    /*if (adjust_invalid_count == 3)
                                    {
                                        WhiteBalanceAdjustFail(StepNow, "AdjustValid");
                                    }
                                    else
                                    {
                                        DealTvReturnOk();
                                    }*/
                                }
                                else
                                {
                                    adjust_invalid_count = 0;
                                }
                                
                            }

                            /* R值与X成正比，当测到X大于目标的X，减小R值 */
                            CurRGBValue.RValue += (byte)(CurColorTemp.X > TarColorTemp.X ? -StepX : StepX);
                            GaimIsWithinRange();

                            /* 发送命令 */
                            Trigger_SendCmdEvent(this, new SendCmdEventArgs(CurRGBValue));
                            return;
                        }

                        if (CommStatu.RetOk)
                            DealTvReturnOk();
                        else {
                            CurRGBValue.RValue += (byte)(CurColorTemp.X > TarColorTemp.X ? StepX : -StepX);
                            DealTvReturnErr();
                        }
                    }
                    break;

                case AdjustStep.AdjustY_B: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {

                            if (CommStatu.RetTimeOut) {
                                ResetFlag();
                                CurRGBValue.BValue += (byte)(CurColorTemp.Y > TarColorTemp.Y ? -StepY : StepY);
                            }

                            if (FirstJudge) {
                                FirstJudge = false;
                            } else {
                                /* 第一次调整的时候并不会进来，因为能进来调整肯定是因为之前已经测量过并不达标 */
                                if (GetColorTemp())
                                    return;

                                if (CheckIsYOk())
                                {
                                    /* 调整完Y后如果X也是合格的，就判断为成功 */
                                    if (CheckIsXOk())
                                    {
                                        StepNow = AdjustStep.AdjustSuccess;
                                    }
                                    else
                                    {
                                        StepNow = AdjustStep.AdjustX_R;
                                    }

                                    FirstJudge = true;
                                    DealTvReturnOk();
                                    break;
                                }

                                /* 判断B值饱和了 */
                                if (CurColorTemp.Y > TarColorTemp.Y && Math.Abs(PreColorTemp.Y - CurColorTemp.Y) < StepY * 2)
                                {
                                    FirstJudge = true;
                                    CurRGBValue.BValue -= StepY;
                                    B_Status = RGB_STATUS.MAX;
                                    WhiteBalanceAdjustFail(StepNow);
									return;
                                }
                                //B值减到最小了
                                if (CurColorTemp.Y < TarColorTemp.Y && ((CurRGBValue.BValue - StepY) < MinRGBGaim.BValue))
                                {
                                    FirstJudge = true;
                                    B_Status = RGB_STATUS.MIN;
                                    WhiteBalanceAdjustFail(StepNow);
                                    return;
                                }
                                Boolean AdjustValid = (CurColorTemp.Y > TarColorTemp.Y) ? (PreColorTemp.Y > CurColorTemp.Y) : (PreColorTemp.Y < CurColorTemp.Y);
                                if (AdjustValid == false)
                                {
                                    adjust_invalid_count++;
									LogHelper.WriteInfoNdc(StepNow + "AdjustValid", adjust_invalid_count.ToString());
                                    /*if (adjust_invalid_count == 3)
                                    {
                                        WhiteBalanceAdjustFail(StepNow, "AdjustValid");
                                    }
                                    else
                                    {
                                        DealTvReturnOk();
                                    }*/
                                }
                                else
                                {
                                    adjust_invalid_count = 0;
                                }

                            }

                            /* B值与Y成反比，当测到的Y大于目标的Y，增加B值 */
                            CurRGBValue.BValue += (byte)(CurColorTemp.Y > TarColorTemp.Y ? StepY : -StepY);
                            GaimIsWithinRange();

                            /* 发送命令 */
                            Trigger_SendCmdEvent(this, new SendCmdEventArgs(CurRGBValue));
                            return;
                        }
                        if (CommStatu.RetOk)
                            DealTvReturnOk();
                        else {
                            CurRGBValue.BValue += (byte)(CurColorTemp.Y > TarColorTemp.Y ? -StepY : StepY);
                            DealTvReturnErr();
                        }
                    }
                    break;

                case AdjustStep.AdjustY_G: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {
                            if (CommStatu.RetTimeOut) {
                                ResetFlag();
                                CurRGBValue.GValue += (byte)(CurColorTemp.Y > TarColorTemp.Y ? 1 : -1);
                            }

                            if (FirstJudge) {
                                FirstJudge = false;
                            } else {
                                if (GetColorTemp())
                                    return;

                                if (CheckIsYOk())
                                {
                                    if (CheckIsXOk())
                                    {
                                        StepNow = AdjustStep.AdjustSuccess;
                                    }
                                    else
                                    {
                                        StepNow = AdjustStep.AdjustX_R;
                                    }
                                    FirstJudge = true;
                                    DealTvReturnOk();
                                    break;
                                }

                                /* 判断G值是否饱和 */
                                if ((CurColorTemp.Y < TarColorTemp.Y) && Math.Abs(PreColorTemp.Y - CurColorTemp.Y) < StepY * 2)
                                {
                                    FirstJudge = true;
                                    CurRGBValue.GValue -= StepY;
                                    G_Status = RGB_STATUS.MAX;
                                    if ((B_Status == RGB_STATUS.MIN) || (R_Status != RGB_STATUS.NORMAL))
                                    {
                                        WhiteBalanceAdjustFail(StepNow);
                                        return;
                                    }
                                    else
                                    {
                                        StepNow = AdjustStep.AdjustY_B;
                                        DealTvReturnOk();
                                        break;
                                    }
                                }
                                //G值达到最小
                                if ((CurColorTemp.Y > TarColorTemp.Y) && ((CurRGBValue.GValue - StepY) < MinRGBGaim.GValue))
                                {
                                    FirstJudge = true;
                                    G_Status = RGB_STATUS.MIN;
                                    if ((R_Status != RGB_STATUS.NORMAL) || (B_Status == RGB_STATUS.MAX))
                                    {
                                        WhiteBalanceAdjustFail(StepNow);
                                        return;
                                    }
                                    else
                                    {
                                        StepNow = AdjustStep.AdjustY_B;
                                        DealTvReturnOk();
                                        break;
                                    }
                                }
                                Boolean AdjustValid = (CurColorTemp.Y > TarColorTemp.Y) ? (PreColorTemp.Y > CurColorTemp.Y) : (PreColorTemp.Y < CurColorTemp.Y);
                                if (AdjustValid == false)
                                {
                                    adjust_invalid_count++;
									LogHelper.WriteInfoNdc(StepNow + "AdjustValid", adjust_invalid_count.ToString());
                                    /*if (adjust_invalid_count == 3)
                                    {
                                        WhiteBalanceAdjustFail(StepNow, "AdjustValid");
                                    }
                                    else
                                    {
                                        DealTvReturnOk();
                                    }*/
                                }
                                else
                                {
                                    adjust_invalid_count = 0;
                                }
                            }

                            CurRGBValue.GValue += (byte)(CurColorTemp.Y > TarColorTemp.Y ? -StepY : StepY);
                            GaimIsWithinRange();
                            Trigger_SendCmdEvent(this, new SendCmdEventArgs(CurRGBValue));
                            return;
                        }
                        if (CommStatu.RetOk)
                            DealTvReturnOk();
                        else {
                            CurRGBValue.GValue += (byte)(CurColorTemp.Y > TarColorTemp.Y ? 1 : -1);
                            DealTvReturnErr();
                        }
                    }
                    break;

                case AdjustStep.AdjustX_R: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {
                            if (CommStatu.RetTimeOut) {
                                ResetFlag();
                                CurRGBValue.RValue += (byte)(CurColorTemp.X > TarColorTemp.X ? StepX : -StepX);
                            }

                            if (FirstJudge)
                                FirstJudge = false;
                            else
                            {
                                if (GetColorTemp())
                                    return;

                                if (CheckIsXOk())
                                {
                                    if (CheckIsYOk())
                                    {
                                        StepNow = AdjustStep.AdjustSuccess;
                                    }
                                    else
                                    {
                                        WhiteBalanceAdjustFail(StepNow);
                                        return;
                                    }
                                    FirstJudge = true;
                                    DealTvReturnOk();
                                    break;
                                }

                                /* R值已经调到饱和 */
                                if (CurColorTemp.X < TarColorTemp.X && Math.Abs(PreColorTemp.X - CurColorTemp.X) < StepX * 2)
                                {
                                    FirstJudge = true;
                                    CurRGBValue.RValue -= StepX;
                                    R_Status = RGB_STATUS.MAX;
                                    if (B_Status == RGB_STATUS.MIN)
                                    {
                                        WhiteBalanceAdjustFail(StepNow);
                                        return;
                                    }
                                    else
                                    {
                                        StepNow = AdjustStep.AdjustX_B;
                                        DealTvReturnOk();
                                        break;
                                    }
                                }
                                //R值达到最小
                                if ((CurColorTemp.X > TarColorTemp.X) && ((CurRGBValue.RValue - StepX) < MinRGBGaim.RValue))
                                {
                                    FirstJudge = true;
                                    R_Status = RGB_STATUS.MIN;
                                    if (B_Status == RGB_STATUS.MAX)
                                    {
                                        WhiteBalanceAdjustFail(StepNow);
                                        return;
                                    }
                                    else
                                    {
                                        StepNow = AdjustStep.AdjustX_B;
                                        DealTvReturnOk();
                                        break;
                                    }


                                }
                                Boolean AdjustValid = (CurColorTemp.X > TarColorTemp.X) ? (PreColorTemp.X > CurColorTemp.X) : (PreColorTemp.X < CurColorTemp.X);
                                if (AdjustValid == false)
                                {
                                    adjust_invalid_count++;
									LogHelper.WriteInfoNdc(StepNow + "AdjustValid", adjust_invalid_count.ToString());
                                    /*if (adjust_invalid_count == 3)
                                    {
                                        WhiteBalanceAdjustFail(StepNow, "AdjustValid");
                                    }
                                    else
                                    {
                                        DealTvReturnOk();
                                    }*/
                                }
                                else
                                {
                                    adjust_invalid_count = 0;
                                }

                            }

                            CurRGBValue.RValue += (byte)(CurColorTemp.X > TarColorTemp.X ? -StepX : StepX);
                            GaimIsWithinRange();
                            Trigger_SendCmdEvent(this, new SendCmdEventArgs(CurRGBValue));
                            return;
                        }
                        if (CommStatu.RetOk)
                            DealTvReturnOk();
                        else {
                            CurRGBValue.RValue += (byte)(CurColorTemp.X > TarColorTemp.X ? StepX : -StepX);
                            DealTvReturnErr();
                        }
                    }
                    break;

                case AdjustStep.AdjustX_B: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {
                            if (CommStatu.RetTimeOut) {
                                ResetFlag();
                                CurRGBValue.BValue += (byte)(CurColorTemp.X > TarColorTemp.X ? -StepX : StepX);
                            }

                            if (FirstJudge)
                                FirstJudge = false;
                            else
                            {
                                if (GetColorTemp())
                                    return;

                                if (CheckIsXOk())
                                {
                                    if (CheckIsYOk())
                                    {
                                        StepNow = AdjustStep.AdjustSuccess;// : AdjustStep.AdjustY_G;
                                        DealTvReturnOk();
                                    }
                                    else
                                    {
                                        WhiteBalanceAdjustFail(StepNow);
                                        return;
                                    }
                                    break;
                                }

                                //B饱和
                                if (CurColorTemp.X > TarColorTemp.X && Math.Abs(PreColorTemp.X - CurColorTemp.X) < StepX * 2)
                                {
                                    FirstJudge = true;
                                    CurRGBValue.BValue -= StepX;
                                    B_Status = RGB_STATUS.MAX;
                                    if (R_Status == RGB_STATUS.MIN)
                                    {
                                        WhiteBalanceAdjustFail(StepNow);
                                        return;
                                    }
                                    else
                                    {
                                        StepNow = AdjustStep.AdjustX_R;
                                        DealTvReturnOk();
                                        return;
                                    }


                                }
                                //B最小
                                if ((CurColorTemp.X < TarColorTemp.X) && ((CurRGBValue.BValue - StepX) < MinRGBGaim.BValue))
                                {
                                    FirstJudge = true;
                                    B_Status = RGB_STATUS.MIN;
                                    if (R_Status == RGB_STATUS.MAX)
                                    {
                                        WhiteBalanceAdjustFail(StepNow);
                                        return;
                                    }
                                    else
                                    {
                                        StepNow = AdjustStep.AdjustX_R;
                                        DealTvReturnOk();
                                        break;
                                    }
                                }
                                Boolean AdjustValid = (CurColorTemp.X > TarColorTemp.X) ? (PreColorTemp.X > CurColorTemp.X) : (PreColorTemp.X < CurColorTemp.X);
                                if (AdjustValid == false)
                                {
                                    adjust_invalid_count++;
									LogHelper.WriteInfoNdc(StepNow + "AdjustValid", adjust_invalid_count.ToString());
                                    /*if (adjust_invalid_count == 3)
                                    {
                                        WhiteBalanceAdjustFail(StepNow, "AdjustValid");
                                    }
                                    else
                                    {
                                        DealTvReturnOk();
                                    }*/
                                }
                                else
                                {
                                    adjust_invalid_count = 0;
                                }

                            }

                            CurRGBValue.BValue += (byte)(CurColorTemp.X > TarColorTemp.X ? StepX : -StepX);
                            GaimIsWithinRange();
                            Trigger_SendCmdEvent(this, new SendCmdEventArgs(CurRGBValue));
                            return;
                        }
                        if (CommStatu.RetOk)
                            DealTvReturnOk();
                        else {
                            CurRGBValue.BValue += (byte)(CurColorTemp.X > TarColorTemp.X ? -2 : 2);
                            DealTvReturnErr();
                        }
                    }
                    break;

                case AdjustStep.LetXLessThanY: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {
                            if (CommStatu.RetTimeOut) {
                                ResetFlag();
                                if (NeedReduceBGaim)
                                    CurRGBValue.BValue += 1;
                                if (NeedReduceRGaim)
                                    CurRGBValue.RValue += 2;
                            }

                            if (FirstJudge) {
                                FirstJudge = false;
                            } else {
                                if (GetColorTemp())
                                    return;

                                if (CheckIsXAndYOK()) {
                                    if (CurColorTemp.Y > CurColorTemp.X) {
                                        FirstJudge = true;
                                        StepNow = AdjustStep.AdjustSuccess;
                                        DealTvReturnOk();
                                        return;
                                    }
                                } else {
                                    if (CurColorTemp.X < TarColorTemp.X - ErrLimit) {
                                        CurRGBValue.RValue += 2;
                                        NeedReduceRGaim = false;
                                    } else if (CurColorTemp.Y > TarColorTemp.Y + ErrLimit) {
                                        CurRGBValue.BValue += 1;
                                        NeedReduceBGaim = false;
                                    }
                                }
                            }

                            if (NeedReduceRGaim)
                                CurRGBValue.RValue -= 2;
                            if (NeedReduceBGaim)
                                CurRGBValue.BValue -= 1;

                            /* 减B会影响到X值，如果之前X值已经判断为低于错误范围，
                             * 然后只操作B值，Y值此时如果已经高于误差范围，还是低于X值
                             * 此时只操作R值来减少X值应该就可以了
                             */
                            if (!NeedReduceRGaim && !NeedReduceBGaim)
                                NeedReduceRGaim = true;

                            GaimIsWithinRange();

                            if (IsSharp648) {
                                StepNow = AdjustStep.LetXLessThanYSetSharp648RValue;
                                DealTvReturnOk();
                            }
                            else {
                                Trigger_SendCmdEvent(this, new SendCmdEventArgs(CurRGBValue));
                            }
                            return;
                        }

                        if (CommStatu.RetOk) {
                            DealTvReturnOk();
                        } else {
                            if (NeedReduceBGaim)
                                CurRGBValue.BValue += 1;
                            if (NeedReduceRGaim)
                                CurRGBValue.RValue += 2;
                            DealTvReturnErr();
                        }
                    }
                    break;

                /* 调整成功 */
                case AdjustStep.AdjustSuccess: {
                        /* 判断是否X大于Y值 */
                        if (CurColorTemp.X > CurColorTemp.Y) {
                            FirstJudge = true;
                            StepNow = AdjustStep.LetXLessThanY;
                            DealTvReturnOk();
                            return;
                        }

                        /* 亮度不达标 */
                        /*if (CtrLv && MesGet && Isstandard)  //mes获取数据勾选
                        {
                            Console.WriteLine("打印一下算法里的mes系统值："+MESisrealtek);
                            if ( CurColorTemp.Lv< (MESisrealtek-40)*0.75) {
                                Trigger_AdjustEvent(this, new AdjustEventArgs(WhiteBalanceAdjustStatus.LvUnaccept, GetMultLangStr.GetStr(LanguageInfo.proRunMultLangXmlPath, "StrCurrentBrightness", LanguageInfo.language) +
                              CurColorTemp.Lv + GetMultLangStr.GetStr(LanguageInfo.proRunMultLangXmlPath, "StrMesLessBrightnessControl", LanguageInfo.language) + MESisrealtek));
                                return;
                            }

                        }else*/
						
						if (CtrLv && CurColorTemp.Lv < TarColorTemp.Lv) {
                            Trigger_AdjustEvent(this, new AdjustEventArgs(WhiteBalanceAdjustStatus.LvUnaccept, GetMultLangStr.GetStr(LanguageInfo.proRunMultLangXmlPath, "StrCurrentBrightness", LanguageInfo.language) +
                                CurColorTemp.Lv + GetMultLangStr.GetStr(LanguageInfo.proRunMultLangXmlPath, "StrLessBrightnessControl", LanguageInfo.language) + TarColorTemp.Lv));
                            return;
                        }

                        /* 计算前后亮度百分百 */
                        double persent = Convert.ToDouble(CurColorTemp.Lv) / Convert.ToDouble(MaxLv);

                        if (CtrMaxCurLvRadio && persent > 1) {
                            string errInfo = "    MaxLv: " + MaxLv + " / " + " Lv: " + CurColorTemp.Lv + "> 1 ple check!";
                            Trigger_AdjustEvent(this, new AdjustEventArgs(WhiteBalanceAdjustStatus.LvHighMaxLv, errInfo));
                        } else {
                            WhiteBalanceAdjustStatus adjustStatus = persent < 0.75 ? WhiteBalanceAdjustStatus.LvPercentageLow : WhiteBalanceAdjustStatus.AdjustSuccess;
                            string info = string.Empty;
                            if (MaxLv == 0)
                            {
                                info = "  R:" + CurRGBValue.RValue + "  G:" + CurRGBValue.GValue + "  B:" + CurRGBValue.BValue;
                            }
                            else
                            {
                                info = "  " + persent.ToString("0%") + "  R:" + CurRGBValue.RValue + "  G:" + CurRGBValue.GValue + "  B:" + CurRGBValue.BValue;
                            }

                            Trigger_AdjustEvent(this, new AdjustEventArgs(adjustStatus, info, CurColorTemp, CurRGBValue));
                        }
                    }
                    break;

                case AdjustStep.CheckRPosiDirSatur: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {
                            Trigger_SendCmdEvent(this, new SendCmdEventArgs(CurRGBValue));
                            return;
                        }

                        if (CommStatu.RetOk) {
                            if (GetColorTemp())
                                return;

                            /* 是否饱和 */
                            if (PreColorTemp.X - CurColorTemp.X < 5) {
                                CurRGBValue.RValue -= 1;
                                GaimIsWithinRange();
                            } else {
                                /* 
                                 * 为了下一个 PreColorTemp = CurColorTemp 做准备
                                 * 因为R值已经不是饱和了，证明上一次没减之前是好的
                                 * 因为退出这个case之前就会加回去，所以下一个case
                                 * 应该要判断的色温是 PreColorTemp
                                 */
                                CurRGBValue.BValue -= 1;
                                CurRGBValue.RValue += 1;
                                CurColorTemp = ExtentionForDeepCopy.DeepCopy(PreColorTemp);
                                FirstJudge = true;
                                StepNow = AdjustStep.CheckBPosiDirSatur;
                            }
                            DealTvReturnOk();
                        } else
                            DealTvReturnErr();
                    }
                    break;

                case AdjustStep.CheckGPosiDirSatur: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {
                            Trigger_SendCmdEvent(this, new SendCmdEventArgs(CurRGBValue));
                            return;
                        }

                        if (CommStatu.RetOk) {
                            if (GetColorTemp())
                                return;

                            /* 是否饱和 */
                            if (PreColorTemp.Y - CurColorTemp.Y < 5) {
                                CurRGBValue.GValue -= 1;
                                GaimIsWithinRange();
                            } else {
                                CurRGBValue.GValue += 1;
                                CurRGBValue.BValue -= 1;
                                CurColorTemp = ExtentionForDeepCopy.DeepCopy(PreColorTemp);
                                FirstJudge = true;
                                StepNow = AdjustStep.CheckBPosiDirSatur;

                            }
                            DealTvReturnOk();
                        } else
                            DealTvReturnErr();
                    }
                    break;

                case AdjustStep.CheckBPosiDirSatur: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {
                            Trigger_SendCmdEvent(this, new SendCmdEventArgs(CurRGBValue));
                            return;
                        }

                        if (CommStatu.RetOk) {
                            if (GetColorTemp())
                                return;

                            /* 是否饱和 */
                            if (CurColorTemp.Y - PreColorTemp.Y < 5) {
                                CurRGBValue.BValue -= 1;
                                GaimIsWithinRange();
                            } else {
                                CurRGBValue.BValue += 1;
                                FirstJudge = true;
                                StepNow = AdjustStep.ComfrimSetRGBToTv;
                            }
                            DealTvReturnOk();
                        } else
                            DealTvReturnErr();
                    }
                    break;

                case AdjustStep.ComfrimSetRGBToTv: {
                        if (CommStatu.SendStatu == CommunicationSendStatus.SendNone) {
                            Trigger_SendCmdEvent(this, new SendCmdEventArgs(CurRGBValue));
                            return;
                        }
                        if (CommStatu.RetOk) {
                            CurColorTemp = ColAnalyzer.GetColorTemp();

                            //UpLoadInfoToMes(StepNow);
                            if (CheckIsXAndYOK())
                                StepNow = AdjustStep.AdjustSuccess;
                            else
                            {
                                //StepNow = CheckIsXOk() ? AdjustStep.AdjustY_B : AdjustStep.AdjustRToTar;
                                StepNow = CheckIsYOk() ? AdjustStep.AdjustRToTar : AdjustStep.AdjustY_B;
                            }
                            FirstJudge = true;
                            DealTvReturnOk();
                        } else
                            DealTvReturnErr();
                    }
                    break;
                //case AdjustStep.AdjustGToOptimal: {
                //    if (Trigger_InqureSendStatusEvent() == CommunicationSendStatus.SendNone) {
                //        if (FirstJudge) {
                //            FirstJudge = false;
                //        } else {
                //            if (GetColorTemp())
                //                return;

                //            if (!CheckIsXAndYOK() || (Math.Abs(PreColorTemp.Y - CurColorTemp.Y) < 5)) {
                //                CurRGBValue.GValue -= 1;
                //                PreColorTemp = ExtentionForDeepCopy.DeepCopy(CurColorTemp);
                //                FirstJudge = true;
                //                StepNow = AdjustStep.SetCurGToTv;
                //                TestAction.Invoke();
                //                return;
                //            }
                //        }

                //        CurRGBValue.GValue += 1;
                //        GaimIsWithinRange();
                //        Trigger_SendCmdEvent(this, new SendCmdEventArgs(GetSetRGBCmd()));
                //        return;
                //    }

                //    if (Trigger_InqureTvOkEvent())
                //        DealTvReturnOk();
                //    else {
                //        CurRGBValue.GValue -= 1;
                //        DealTvReturnErr();
                //    }
                //}
                //break;

                //case AdjustStep.SetCurGToTv: {
                //    if (Trigger_SendCmdEvent(this, new SendCmdEventArgs(GetSetRGBCmd())))
                //        return;
                //    if (Trigger_InqureTvOkEvent()) {
                //        if (GetColorTemp())
                //            return;

                //        if (CheckIsXAndYOK())
                //            StepNow = AdjustStep.AdjustSuccess;
                //        else
                //            StepNow = BGaimIsSaturation ? AdjustStep.AdjustY_G : AdjustStep.AdjustY_B;
                //        TestAction.Invoke();
                //    }
                //}
                //break;

                case AdjustStep.AdjustNone:
                default:
                    if (AdjustNow) {
                        string str = GetMultLangStr.GetStr(LanguageInfo.proRunMultLangXmlPath, "StrStepNull", LanguageInfo.language);
                        Trigger_AdjustEvent(this, new AdjustEventArgs(WhiteBalanceAdjustStatus.AdjustStepNull, str));
                    }
                    break;
            }
        }



        #endregion

    }
}
