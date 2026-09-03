// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./AssetVaultLayout.sol";
import "../../nameserver/nameServiceRef/NameServiceRefLogic.sol";
import "../holders/HERC721HolderLogic.sol";
import "../holders/HERC1155HolderLogic.sol";
import "./AssetVaultInterface.sol";

import "../erc20improved/HERC20IMInterface.sol";
import "../erc1155improved/HERC1155IMInterface.sol";
import "../erc721improved/HERC721IMInterface.sol";

import "./AssetVaultType.sol";

//asset vault only deals deposit and withdraw, perhaps with setAttributes

abstract contract AssetVaultLogic1 is AssetVaultLayout, NameServiceRefLogic, AssetVaultInterface1 {

    using SafeMath for uint256;
    using SafeERC20 for IERC20;

    function _depositErc20(
        bytes32 erc20TokenName,
        address owner,
        uint256 amount
    ) internal {
        bytes32[] memory erc20TokenNames = new bytes32[](1);
        erc20TokenNames[0] = erc20TokenName;
        uint256[] memory amounts = new uint256[](1);
        amounts[0] = amount;

        _depositErc20s(erc20TokenNames, owner, amounts);
    }

    function _depositErc20s(
        bytes32[] memory erc20TokenNames,
        address owner,
        uint256[] memory amounts
    ) internal {
        require(erc20TokenNames.length == amounts.length, "deposit Erc20s, length mismatch");

        for (uint256 i = 0; i < erc20TokenNames.length; i++) {

            bytes32 erc20TokenName = erc20TokenNames[i];
            address erc20Token = ns().getSingleSafe(erc20TokenName);

            uint256 amount = amounts[i];

            uint256 depositStrategy = _erc20DepositStrategy[erc20TokenName];
            if (depositStrategy == AssetVaultType.ERC20_DEPOSIT_STRATEGY_BURN) {

                HERC20IMInterface(erc20Token).burnSudo(owner, amount);

            } else if (depositStrategy == AssetVaultType.ERC20_DEPOSIT_STRATEGY_TRANSFER) {

                address holder = _erc20DepositStrategyAddress[erc20TokenName];
                uint256 temp = IERC20(erc20Token).balanceOf(holder);
                IERC20(erc20Token).safeTransferFrom(owner, holder, amount);
                amount = IERC20(erc20Token).balanceOf(holder).sub(temp);

            } else if (depositStrategy == AssetVaultType.ERC20_DEPOSIT_STRATEGY_STORE) {

                uint256 temp = IERC20(erc20Token).balanceOf(address(this));
                IERC20(erc20Token).safeTransferFrom(owner, address(this), amount);
                amount = IERC20(erc20Token).balanceOf(address(this)).sub(temp);

            } else {
                revert("unsupported token");
            }
        }

        emit DepositErc20(owner, erc20TokenNames, amounts, owner);
    }

    function _withdrawErc20(
        bytes32 erc20TokenName,
        address owner,
        uint256 amount,
        uint256 traceId
    ) internal {

        bytes32[] memory erc20TokenNames = new  bytes32[](1);
        erc20TokenNames[0] = erc20TokenName;

        uint256[] memory amounts = new uint256[](1);
        amounts[0] = amount;

        _withdrawErc20s(erc20TokenNames, owner, amounts, traceId);
    }

    function _withdrawErc20s(
        bytes32[] memory erc20TokenNames,
        address owner,
        uint256[] memory amounts,
        uint256 traceId
    ) internal {

        require(erc20TokenNames.length == amounts.length, "withdraw Erc20s, length mismatch");

        for (uint256 i = 0; i < erc20TokenNames.length; i ++) {

            bytes32 erc20TokenName = erc20TokenNames[i];
            address erc20Token = ns().getSingleSafe(erc20TokenName);

            uint256 amount = _withdraw20Hook(erc20TokenName, owner, amounts[i]);
            amounts[i] = amount;

            uint256 withdrawStrategy = _erc20WithdrawStrategy[erc20TokenName];
            if (withdrawStrategy == AssetVaultType.ERC20_WITHDRAW_STRATEGY_MINT) {

                HERC20IMInterface(erc20Token).mintSudo(owner, amount);

            } else if (withdrawStrategy == AssetVaultType.ERC20_WITHDRAW_STRATEGY_TRANSFER) {

                address holder = _erc20WithdrawStrategyAddress[erc20TokenName];
                IERC20(erc20Token).safeTransferFrom(holder, owner, amount);

            } else if (
                withdrawStrategy == AssetVaultType.ERC20_WITHDRAW_STRATEGY_RESTORE_MINT ||
                withdrawStrategy == AssetVaultType.ERC20_WITHDRAW_STRATEGY_RESTORE_REVERT ||
                withdrawStrategy == AssetVaultType.ERC20_WITHDRAW_STRATEGY_RESTORE_TRANSFER

            ) {
                uint256 bal = IERC20(erc20Token).balanceOf(address(this));
                if (bal < amount) {

                    uint256 gap = amount.sub(bal);

                    if (withdrawStrategy == AssetVaultType.ERC20_WITHDRAW_STRATEGY_RESTORE_MINT) {
                        //mint the remaining
                        HERC20IMInterface(erc20Token).mintSudo(owner, gap);
                    } else if (withdrawStrategy == AssetVaultType.ERC20_WITHDRAW_STRATEGY_RESTORE_REVERT) {
                        revert("insufficient erc20 token for withdraw");
                    } else if (withdrawStrategy == AssetVaultType.ERC20_WITHDRAW_STRATEGY_RESTORE_TRANSFER) {
                        //the address must be approve first
                        address holder = _erc20WithdrawStrategyAddress[erc20TokenName];
                        IERC20(erc20Token).safeTransferFrom(holder, owner, gap);
                    }
                    else {
                        revert("withdrawErc20s unknown withdraw strategy");
                    }
                }
                IERC20(erc20Token).safeTransfer(owner, bal);
            } else {
                revert("unsupported token");
            }
        }

        emit WithdrawErc20(owner, erc20TokenNames, amounts, traceId);
    }


    function _withdraw20Hook(
        bytes32 erc20TokenName,
        address owner,
        uint256 amount
    ) virtual internal returns (uint256 processedAmount){
        erc20TokenName;
        owner;
        return amount;
    }
}
